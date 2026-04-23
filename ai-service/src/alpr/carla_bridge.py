from __future__ import annotations

from dataclasses import dataclass
import time
from typing import Callable, Iterable

import numpy as np

from .pipeline import ALPRPipeline, PlateResult
from .runtime import ALPRService, carla_image_to_bgr


@dataclass
class CameraFrame:
    camera_id: str
    image: np.ndarray


class MultiCameraProcessor:
    def __init__(self, pipeline: ALPRPipeline) -> None:
        self.pipeline = pipeline

    def process(self, frames: Iterable[CameraFrame]) -> list[PlateResult]:
        payload = [(frame.camera_id, frame.image) for frame in frames]
        return self.pipeline.process_batch(payload)


class CarlaFrameHandler:
    def __init__(self, service: ALPRService) -> None:
        self.service = service

    def handle(self, image: object, camera_id: str, frame_id: str | None = None) -> list:
        frame = carla_image_to_bgr(image)
        return self.service.handle_frame(frame, camera_id=camera_id, frame_id=frame_id)


def attach_camera_listener(
    sensor: object,
    camera_id: str,
    handler: Callable[[object, str, str | None], None],
) -> None:
    def _callback(image: object) -> None:
        frame_id = str(getattr(image, "frame", getattr(image, "frame_number", ""))) or None
        handler(image, camera_id, frame_id)

    sensor.listen(_callback)


def stream_from_carla(world: object, camera_ids: list[str]):
    raise NotImplementedError(
        "Spawn CARLA camera sensors in your simulator code, then attach them with attach_camera_listener()."
    )


def _get_vehicle(world: object, vehicle_id: int | None) -> object:
    vehicles = world.get_actors().filter("vehicle.*")
    if vehicle_id is None:
        if len(vehicles) == 0:
            raise RuntimeError("No vehicle actor found in CARLA world")
        return vehicles[0]

    for actor in vehicles:
        if int(actor.id) == int(vehicle_id):
            return actor
    raise RuntimeError(f"Vehicle with id={vehicle_id} was not found")


def run_carla_service(
    service: ALPRService,
    host: str,
    port: int,
    timeout: float,
    vehicle_id: int | None,
    camera_id: str,
    width: int,
    height: int,
    fov: float,
    duration_seconds: float | None,
) -> None:
    try:
        import carla
    except ImportError as exc:
        raise RuntimeError(
            "CARLA Python API is not installed. Install 'carla' in your Python environment."
        ) from exc

    client = carla.Client(host, int(port))
    client.set_timeout(float(timeout))
    world = client.get_world()
    vehicle = _get_vehicle(world, vehicle_id)

    blueprint = world.get_blueprint_library().find("sensor.camera.rgb")
    blueprint.set_attribute("image_size_x", str(int(width)))
    blueprint.set_attribute("image_size_y", str(int(height)))
    blueprint.set_attribute("fov", str(float(fov)))

    # Front camera placement for an ego vehicle style setup.
    transform = carla.Transform(carla.Location(x=1.8, z=1.6), carla.Rotation(pitch=0.0))
    sensor = world.spawn_actor(blueprint, transform, attach_to=vehicle)

    handler = CarlaFrameHandler(service)
    attach_camera_listener(sensor, camera_id=camera_id, handler=handler.handle)

    started = time.time()
    try:
        while True:
            time.sleep(0.05)
            if duration_seconds is not None and (time.time() - started) >= duration_seconds:
                break
    except KeyboardInterrupt:
        pass
    finally:
        sensor.stop()
        sensor.destroy()
