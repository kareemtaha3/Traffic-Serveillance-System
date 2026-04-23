from __future__ import annotations

from dataclasses import dataclass
from typing import Iterable

import numpy as np

from .models.detector_base import Detection, Detector
from .models.recognizer_base import PlateText, Recognizer
from .utils.vision import crop_boxes, ensure_rgb


@dataclass
class PlateResult:
    bbox: tuple[int, int, int, int]
    text: str
    score: float
    camera_id: str | None


class ALPRPipeline:
    def __init__(
        self,
        detector: Detector,
        recognizer: Recognizer,
        min_confidence: float = 0.3,
        max_detections: int = 10,
    ) -> None:
        self.detector = detector
        self.recognizer = recognizer
        self.min_confidence = min_confidence
        self.max_detections = max_detections

    def process_frame(
        self, image: np.ndarray, camera_id: str | None = None
    ) -> list[PlateResult]:
        rgb = ensure_rgb(image)
        detections = self.detector.detect(rgb)
        detections = [d for d in detections if d.score >= self.min_confidence]
        detections = sorted(detections, key=lambda d: d.score, reverse=True)
        detections = detections[: self.max_detections]

        crops = crop_boxes(rgb, [d.bbox for d in detections])
        texts = self.recognizer.recognize(crops)

        results: list[PlateResult] = []
        for det, text in zip(detections, texts, strict=False):
            results.append(
                PlateResult(
                    bbox=det.bbox,
                    text=text.text,
                    score=text.score * det.score,
                    camera_id=camera_id,
                )
            )
        return results

    def process_batch(
        self, frames: Iterable[tuple[str | None, np.ndarray]]
    ) -> list[PlateResult]:
        results: list[PlateResult] = []
        for camera_id, frame in frames:
            results.extend(self.process_frame(frame, camera_id=camera_id))
        return results
