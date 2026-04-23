from __future__ import annotations

import argparse
import json
import threading
import sys
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path

import cv2
import numpy as np

ROOT = Path(__file__).resolve().parent
sys.path.append(str(ROOT / "src"))

from alpr import ALPRPipeline, load_config  # noqa: E402
from alpr.models.onnx_recognizer import OnnxPlateRecognizer  # noqa: E402
from alpr.models.yolo_char_recognizer import YoloCharRecognizer  # noqa: E402
from alpr.models.yolo_detector import YoloV8Detector  # noqa: E402
from alpr.carla_bridge import run_carla_service  # noqa: E402
from alpr.runtime import (  # noqa: E402
    ALPRService,
    HttpBackendPublisher,
    NullBackendPublisher,
    build_self_test_pipeline,
    carla_image_to_bgr,
)


def _resolve_artifact_path(config_path: Path, path_value: str, kind: str) -> Path:
    raw = Path(path_value)
    if raw.is_absolute():
        if not raw.exists():
            raise SystemExit(f"Configured {kind} path does not exist: {raw}")
        return raw

    candidates = [
        config_path.parent / raw,
        ROOT / raw,
        ROOT / "artifacts" / raw,
    ]

    if kind == "detector":
        candidates.append(ROOT / "artifacts" / "models" / "plate-detector.pt")
    elif kind == "recognizer":
        candidates.append(ROOT / "artifacts" / "models" / "plate-characters.pt")
    elif kind == "charset":
        candidates.append(ROOT / "data" / "processed" / "recognition" / "charset.txt")

    for candidate in candidates:
        if candidate.exists():
            return candidate

    raise SystemExit(
        f"Configured {kind} path '{path_value}' was not found. Tried: "
        + ", ".join(str(path) for path in candidates)
    )


def build_pipeline(config_path: Path) -> ALPRPipeline:
    config = load_config(config_path)
    detector_path = _resolve_artifact_path(
        config_path, config.models.detector_path, kind="detector"
    )
    recognizer_path = _resolve_artifact_path(
        config_path, config.models.recognizer_path, kind="recognizer"
    )

    if config.models.detector_type.lower() != "yolov8":
        raise SystemExit(f"Unsupported detector type: {config.models.detector_type}")
    detector = YoloV8Detector(str(detector_path), device=config.models.device)

    recognizer_type = config.models.recognizer_type.lower()
    if recognizer_type == "onnx":
        recognizer = OnnxPlateRecognizer(
            str(recognizer_path), charset=config.models.charset
        )
    elif recognizer_type in {"yolov8-chars", "yolov8_chars", "yolo-chars"}:
        recognizer = YoloCharRecognizer(
            str(recognizer_path),
            charset=config.models.charset,
            device=config.models.device,
        )
    else:
        raise SystemExit(f"Unsupported recognizer type: {config.models.recognizer_type}")

    return ALPRPipeline(
        detector=detector,
        recognizer=recognizer,
        min_confidence=config.pipeline.min_confidence,
        max_detections=config.pipeline.max_detections,
    )


def run_single_image(pipeline: ALPRPipeline, image_path: Path, camera_id: str | None) -> None:
    image = cv2.imread(str(image_path))
    if image is None:
        raise SystemExit(f"Failed to read image: {image_path}")
    results = pipeline.process_frame(image, camera_id=camera_id)
    print(json.dumps([result.__dict__ for result in results], indent=2, ensure_ascii=False))


def run_self_test() -> None:
    pipeline = build_self_test_pipeline()
    service = ALPRService(pipeline, publisher=NullBackendPublisher(), source="self-test")
    image = np.zeros((720, 1280, 3), dtype=np.uint8)
    results = service.handle_frame(image, camera_id="test-camera", frame_id="frame-0001")
    print(json.dumps([event.to_payload() for event in results], indent=2, ensure_ascii=False))


def run_local_integration_test() -> None:
    received: list[dict[str, object]] = []

    class _Handler(BaseHTTPRequestHandler):
        def do_POST(self) -> None:  # noqa: N802
            content_length = int(self.headers.get("Content-Length", "0"))
            body = self.rfile.read(content_length)
            payload = json.loads(body.decode("utf-8"))
            received.append(payload)

            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.end_headers()
            self.wfile.write(b'{"ok": true}')

        def log_message(self, fmt: str, *args) -> None:
            return

    server = HTTPServer(("127.0.0.1", 0), _Handler)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()

    try:
        endpoint = f"http://127.0.0.1:{server.server_port}/plates"
        publisher = HttpBackendPublisher(endpoint, timeout=2.0)
        pipeline = build_self_test_pipeline()
        service = ALPRService(pipeline, publisher=publisher, source="integration-test")

        image = np.zeros((480, 640, 3), dtype=np.uint8)
        events = service.handle_frame(
            image, camera_id="integration-camera", frame_id="frame-integration-0001"
        )

        if not events:
            raise SystemExit("Integration test failed: no events produced by pipeline")
        if not received:
            raise SystemExit("Integration test failed: no payload received by mock backend")

        print(
            json.dumps(
                {
                    "status": "ok",
                    "event_count": len(events),
                    "received_payloads": received,
                },
                indent=2,
                ensure_ascii=False,
            )
        )
    finally:
        server.shutdown()
        server.server_close()


def build_backend_publisher(backend_url: str | None, backend_timeout: float):
    if not backend_url:
        return NullBackendPublisher()
    return HttpBackendPublisher(backend_url, timeout=backend_timeout)


def main() -> None:
    parser = argparse.ArgumentParser(description="ALPR inference")
    parser.add_argument(
        "--config",
        type=Path,
        default=Path("configs/pipeline.yaml"),
        help="Path to config YAML",
    )
    parser.add_argument("--image", type=Path, help="Path to input image")
    parser.add_argument("--camera-id", type=str, default=None)
    parser.add_argument(
        "--backend-url",
        type=str,
        default=None,
        help="Optional backend endpoint to receive recognized plate events",
    )
    parser.add_argument(
        "--backend-timeout",
        type=float,
        default=5.0,
        help="Timeout in seconds for backend requests",
    )
    parser.add_argument(
        "--self-test",
        action="store_true",
        help="Run a deterministic pipeline test without CARLA or backend",
    )
    parser.add_argument(
        "--integration-test",
        action="store_true",
        help="Run full local test with fake pipeline + mock backend server",
    )
    parser.add_argument(
        "--carla",
        action="store_true",
        help="Receive frames from CARLA sensors and run ALPR continuously",
    )
    parser.add_argument("--carla-host", type=str, default="127.0.0.1")
    parser.add_argument("--carla-port", type=int, default=2000)
    parser.add_argument("--carla-timeout", type=float, default=10.0)
    parser.add_argument("--carla-vehicle-id", type=int, default=None)
    parser.add_argument("--carla-camera-id", type=str, default="cam-front")
    parser.add_argument("--carla-width", type=int, default=1280)
    parser.add_argument("--carla-height", type=int, default=720)
    parser.add_argument("--carla-fov", type=float, default=90.0)
    parser.add_argument(
        "--carla-duration",
        type=float,
        default=None,
        help="Optional number of seconds to run; defaults to until Ctrl+C",
    )
    args = parser.parse_args()

    if args.self_test:
        run_self_test()
        return

    if args.integration_test:
        run_local_integration_test()
        return

    pipeline = build_pipeline(args.config)
    publisher = build_backend_publisher(args.backend_url, args.backend_timeout)

    if args.carla:
        service = ALPRService(pipeline, publisher=publisher, source="carla")
        run_carla_service(
            service=service,
            host=args.carla_host,
            port=args.carla_port,
            timeout=args.carla_timeout,
            vehicle_id=args.carla_vehicle_id,
            camera_id=args.carla_camera_id,
            width=args.carla_width,
            height=args.carla_height,
            fov=args.carla_fov,
            duration_seconds=args.carla_duration,
        )
        return

    if args.image:
        service = ALPRService(pipeline, publisher=publisher)
        image = cv2.imread(str(args.image))
        if image is None:
            raise SystemExit(f"Failed to read image: {args.image}")
        image = carla_image_to_bgr(image)
        results = service.handle_frame(image, camera_id=args.camera_id, frame_id=args.image.name)
        print(json.dumps([event.to_payload() for event in results], indent=2, ensure_ascii=False))
    else:
        raise SystemExit(
            "Provide --image, or use --carla, --self-test, or --integration-test."
        )


if __name__ == "__main__":
    main()
