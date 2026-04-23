from __future__ import annotations

import numpy as np

from .recognizer_base import PlateText, Recognizer
from ..utils.vision import normalize_plate, resize_keep_ratio


class OnnxPlateRecognizer(Recognizer):
    def __init__(self, model_path: str, charset: str) -> None:
        try:
            import onnxruntime as ort
        except ImportError as exc:  # pragma: no cover - optional dependency
            raise ImportError("onnxruntime is required for OnnxPlateRecognizer") from exc

        self.session = ort.InferenceSession(model_path)
        self.charset = charset
        self.input_name = self.session.get_inputs()[0].name

    def _decode(self, logits: np.ndarray) -> PlateText:
        indices = np.argmax(logits, axis=-1)
        chars = [self.charset[i] for i in indices if i < len(self.charset)]
        text = "".join(chars)
        score = float(np.max(logits))
        return PlateText(text=text, score=score)

    def recognize(self, crops: list[np.ndarray]) -> list[PlateText]:
        results: list[PlateText] = []
        for crop in crops:
            crop = resize_keep_ratio(crop, target=(160, 48))
            crop = normalize_plate(crop)
            crop = np.expand_dims(crop, axis=0)
            logits = self.session.run(None, {self.input_name: crop})[0]
            results.append(self._decode(logits[0]))
        return results
