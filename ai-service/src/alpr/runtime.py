from __future__ import annotations

from dataclasses import asdict, dataclass, field
import time
from typing import Protocol

import numpy as np

from .models.detector_base import Detection
from .models.recognizer_base import PlateText
from .pipeline import ALPRPipeline


@dataclass
class PlateEvent:
    plate_text: str
    score: float
    bbox: tuple[int, int, int, int]
    camera_id: str | None
    frame_id: str | None
    source: str
    timestamp: float = field(default_factory=time.time)

    def to_payload(self) -> dict[str, object]:
        payload = asdict(self)
        payload["plate"] = self.plate_text
        return payload


class BackendPublisher(Protocol):
    def publish(self, event: PlateEvent) -> None:
        raise NotImplementedError


class NullBackendPublisher:
    def publish(self, event: PlateEvent) -> None:
        return None


class HttpBackendPublisher:
    def __init__(self, endpoint: str, timeout: float = 5.0) -> None:
        try:
            import requests
        except ImportError as exc:  # pragma: no cover - optional dependency
            raise ImportError("requests is required for HttpBackendPublisher") from exc

        self._requests = requests
        self.endpoint = endpoint
        self.timeout = timeout

    def publish(self, event: PlateEvent) -> None:
        response = self._requests.post(
            self.endpoint,
            json=event.to_payload(),
            timeout=self.timeout,
        )
        response.raise_for_status()


class ALPRService:
    def __init__(
        self,
        pipeline: ALPRPipeline,
        publisher: BackendPublisher | None = None,
        source: str = "carla",
    ) -> None:
        self.pipeline = pipeline
        self.publisher = publisher or NullBackendPublisher()
        self.source = source

    def handle_frame(
        self,
        image: np.ndarray,
        camera_id: str | None = None,
        frame_id: str | None = None,
    ) -> list[PlateEvent]:
        results = self.pipeline.process_frame(image, camera_id=camera_id)
        events: list[PlateEvent] = []
        for result in results:
            event = PlateEvent(
                plate_text=result.text,
                score=result.score,
                bbox=result.bbox,
                camera_id=result.camera_id,
                frame_id=frame_id,
                source=self.source,
            )
            self.publisher.publish(event)
            events.append(event)
        return events


def carla_image_to_bgr(image: object) -> np.ndarray:
    if isinstance(image, np.ndarray):
        return image

    raw_data = getattr(image, "raw_data", None)
    width = getattr(image, "width", None)
    height = getattr(image, "height", None)
    if raw_data is None:
        raise TypeError("Expected a CARLA image with raw_data, width, and height")
    if width is None or height is None:
        raise TypeError("Expected a CARLA image with raw_data, width, and height")

    width = int(width)
    height = int(height)

    frame = np.frombuffer(raw_data, dtype=np.uint8).reshape((height, width, 4))
    return frame[:, :, :3].copy()


def build_self_test_pipeline() -> ALPRPipeline:
    class _FakeDetector:
        def detect(self, image: np.ndarray) -> list[Detection]:
            height, width = image.shape[:2]
            return [
                Detection(
                    bbox=(width // 4, height // 3, (3 * width) // 4, (2 * height) // 3),
                    score=0.99,
                    label="plate",
                )
            ]

    class _FakeRecognizer:
        def recognize(self, crops: list[np.ndarray]) -> list[PlateText]:
            return [PlateText(text="أ ه م 2 1 2 ", score=0.98) for _ in crops]

    return ALPRPipeline(
        detector=_FakeDetector(),
        recognizer=_FakeRecognizer(),
        min_confidence=0.0,
        max_detections=1,
    )