from __future__ import annotations

from dataclasses import dataclass
from typing import Protocol

import numpy as np


@dataclass
class Detection:
    bbox: tuple[int, int, int, int]
    score: float
    label: str


class Detector(Protocol):
    def detect(self, image: np.ndarray) -> list[Detection]:
        raise NotImplementedError
