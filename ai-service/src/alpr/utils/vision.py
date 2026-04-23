from __future__ import annotations

from typing import Iterable

import cv2
import numpy as np


def ensure_rgb(image: np.ndarray) -> np.ndarray:
    if image.ndim == 2:
        return cv2.cvtColor(image, cv2.COLOR_GRAY2RGB)
    if image.shape[2] == 4:
        return cv2.cvtColor(image, cv2.COLOR_BGRA2RGB)
    return cv2.cvtColor(image, cv2.COLOR_BGR2RGB)


def crop_boxes(image: np.ndarray, boxes: Iterable[tuple[int, int, int, int]]) -> list[np.ndarray]:
    crops: list[np.ndarray] = []
    h, w = image.shape[:2]
    for x1, y1, x2, y2 in boxes:
        x1 = max(0, min(w - 1, x1))
        x2 = max(0, min(w, x2))
        y1 = max(0, min(h - 1, y1))
        y2 = max(0, min(h, y2))
        if x2 <= x1 or y2 <= y1:
            continue
        crops.append(image[y1:y2, x1:x2])
    return crops


def resize_keep_ratio(image: np.ndarray, target: tuple[int, int]) -> np.ndarray:
    target_w, target_h = target
    h, w = image.shape[:2]
    scale = min(target_w / w, target_h / h)
    new_w, new_h = int(w * scale), int(h * scale)
    resized = cv2.resize(image, (new_w, new_h))
    canvas = np.zeros((target_h, target_w, 3), dtype=resized.dtype)
    x_offset = (target_w - new_w) // 2
    y_offset = (target_h - new_h) // 2
    canvas[y_offset : y_offset + new_h, x_offset : x_offset + new_w] = resized
    return canvas


def normalize_plate(image: np.ndarray) -> np.ndarray:
    image = image.astype("float32") / 255.0
    return image.transpose(2, 0, 1)
