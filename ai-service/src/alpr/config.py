from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import Any

import yaml


@dataclass
class ModelConfig:
    detector_type: str
    detector_path: str
    recognizer_type: str
    recognizer_path: str
    charset: str
    charset_path: str | None
    device: str


@dataclass
class PipelineConfig:
    min_confidence: float
    max_detections: int
    input_size: tuple[int, int]


@dataclass
class CarlaConfig:
    enabled: bool
    host: str
    port: int
    camera_ids: list[str]


@dataclass
class AppConfig:
    models: ModelConfig
    pipeline: PipelineConfig
    carla: CarlaConfig


def _require(data: dict[str, Any], key: str) -> Any:
    if key not in data:
        raise KeyError(f"Missing required config key: {key}")
    return data[key]


def _load_charset(models: dict[str, Any]) -> tuple[str, str | None]:
    charset_path = models.get("charset_path")
    if charset_path:
        path = Path(charset_path)
        lines = [line.strip() for line in path.read_text(encoding="utf-8").splitlines()]
        charset = "".join([line for line in lines if line])
        return charset, str(path)
    return _require(models, "charset"), None


def load_config(path: str | Path) -> AppConfig:
    config_path = Path(path)
    raw = yaml.safe_load(config_path.read_text(encoding="utf-8"))

    models = _require(raw, "models")
    pipeline = _require(raw, "pipeline")
    carla = _require(raw, "carla")

    charset, charset_path = _load_charset(models)

    return AppConfig(
        models=ModelConfig(
            detector_type=_require(models, "detector_type"),
            detector_path=_require(models, "detector_path"),
            recognizer_type=_require(models, "recognizer_type"),
            recognizer_path=_require(models, "recognizer_path"),
            charset=charset,
            charset_path=charset_path,
            device=_require(models, "device"),
        ),
        pipeline=PipelineConfig(
            min_confidence=float(_require(pipeline, "min_confidence")),
            max_detections=int(_require(pipeline, "max_detections")),
            input_size=tuple(_require(pipeline, "input_size")),
        ),
        carla=CarlaConfig(
            enabled=bool(_require(carla, "enabled")),
            host=str(_require(carla, "host")),
            port=int(_require(carla, "port")),
            camera_ids=list(_require(carla, "camera_ids")),
        ),
    )
