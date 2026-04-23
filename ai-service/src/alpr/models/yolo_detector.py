from __future__ import annotations

from typing import Iterable

import numpy as np

from .detector_base import Detection, Detector


class YoloV8Detector(Detector):
    def __init__(self, model_path: str, device: str = "cpu") -> None:
        try:
            from ultralytics import YOLO
        except ImportError as exc:  # pragma: no cover - optional dependency
            raise ImportError(
                "ultralytics is required for YoloV8Detector"
            ) from exc

        self.model = YOLO(model_path)
        self.device = device

    def _to_detections(self, result: object) -> Iterable[Detection]:
        boxes = result.boxes
        names = getattr(result, "names", {})
        for box in boxes:
            conf = float(box.conf[0])
            cls_id = int(box.cls[0])
            label = names.get(cls_id, str(cls_id))
            x1, y1, x2, y2 = box.xyxy[0].tolist()
            yield Detection(
                bbox=(int(x1), int(y1), int(x2), int(y2)),
                score=conf,
                label=label,
            )

    def detect(self, image: np.ndarray) -> list[Detection]:
        results = self.model.predict(image, device=self.device, verbose=False)
        detections: list[Detection] = []
        for result in results:
            detections.extend(self._to_detections(result))
        return detections
