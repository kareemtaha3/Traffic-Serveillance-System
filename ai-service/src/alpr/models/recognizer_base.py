from __future__ import annotations

from dataclasses import dataclass
from typing import Protocol

import numpy as np


@dataclass
class PlateText:
    text: str
    score: float


class Recognizer(Protocol):
    def recognize(self, crops: list[np.ndarray]) -> list[PlateText]:
        raise NotImplementedError
