from __future__ import annotations

import argparse
from pathlib import Path


def resolve_device(device: str) -> str:
    if device != "auto":
        return device
    try:
        import torch

        return "0" if torch.cuda.is_available() else "cpu"
    except Exception:
        return "cpu"


def main() -> None:
    parser = argparse.ArgumentParser(description="Train plate detector with YOLOv8")
    parser.add_argument("--data", type=Path, default=Path("data/processed/detection/data.yaml"))
    parser.add_argument("--model", type=str, default="yolov8n.pt")
    parser.add_argument("--epochs", type=int, default=50)
    parser.add_argument("--img", type=int, default=640)
    parser.add_argument("--device", type=str, default="auto")
    args = parser.parse_args()

    try:
        from ultralytics import YOLO
    except ImportError as exc:  # pragma: no cover
        raise SystemExit("Install ultralytics to train detector") from exc

    device = resolve_device(args.device)
    print(f"Using device: {device}")

    model = YOLO(args.model)
    model.train(
        data=str(args.data),
        epochs=args.epochs,
        imgsz=args.img,
        device=device,
    )


if __name__ == "__main__":
    main()
