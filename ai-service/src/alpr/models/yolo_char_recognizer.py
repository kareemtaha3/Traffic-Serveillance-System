from __future__ import annotations

from typing import Iterable

import numpy as np

from .recognizer_base import PlateText, Recognizer


class YoloCharRecognizer(Recognizer):
    def __init__(self, model_path: str, charset: str, device: str = "cpu") -> None:
        try:
            from ultralytics import YOLO
        except ImportError as exc:  # pragma: no cover - optional dependency
            raise ImportError("ultralytics is required for YoloCharRecognizer") from exc

        self.model = YOLO(model_path)
        self.device = device
        self.charset = list(charset)

    def _class_to_char(self, cls_id: int, names: dict[int, str]) -> str:
        if 0 <= cls_id < len(self.charset):
            return self.charset[cls_id]
        return names.get(cls_id, str(cls_id))

    def _decode(self, result: object) -> PlateText:
        boxes = result.boxes
        names = getattr(result, "names", {})
        chars: list[tuple[float, str, float]] = []
        for box in boxes:
            conf = float(box.conf[0])
            cls_id = int(box.cls[0])
            x1, _, x2, _ = box.xyxy[0].tolist()
            x_center = (x1 + x2) / 2.0
            chars.append((x_center, self._class_to_char(cls_id, names), conf))

        if not chars:
            print("[DEBUG Recognizer] YOLO found 0 individual characters inside the plate crop!")
            return PlateText(text="", score=0.0)

        chars.sort(key=lambda item: item[0])
        text = "".join([item[1] for item in chars])
        score = float(sum(item[2] for item in chars) / len(chars))
        print(f"[DEBUG Recognizer] YOLO found {len(chars)} characters. Ranked text: '{text}', Average Score: {score}")
        return PlateText(text=text, score=score)

    def recognize(self, crops: list[np.ndarray]) -> list[PlateText]:
        print(f"[DEBUG Recognizer] Sending {len(crops)} cropped plates to plate-characters.pt ...")
        results: list[PlateText] = []
        for crop in crops:
            predictions = self.model.predict(crop, device=self.device, verbose=False)
            if not predictions:
                print("[DEBUG Recognizer] predict() returned 0 predictions.")
                results.append(PlateText(text="", score=0.0))
                continue
            results.append(self._decode(predictions[0]))
        return results
