# AI Service Architecture

## Overview
The AI service is a two-stage ALPR pipeline:
1. Plate detection on full vehicle images.
2. Plate character recognition on cropped plates.

Both stages use YOLOv8 models and share a single runtime pipeline that accepts
frames from files or multiple CARLA cameras.

## Data Flow
1. Input frame (image or camera stream).
2. Plate detector finds license plate bounding boxes.
3. Plate crops are extracted.
4. Character recognizer detects individual characters.
5. Characters are sorted left-to-right to build the plate string.

## Datasets
- Vehicle images + plate bounding boxes:
  - dataset/EALPR Vechicles dataset/Vehicles
  - dataset/EALPR Vechicles dataset/Vehicles Labeling
- Plate images + character labels:
  - dataset/EALPR- Plates dataset
  - dataset/EALPR- LP characters dataset/Characters Labeling

## Models
- Plate detector: YOLOv8 detection model trained on vehicle images.
  - Output: plate bounding boxes.
- Character recognizer: YOLOv8 detection model trained on plate images.
  - Output: per-character bounding boxes + class IDs.
  - Decoding: left-to-right sorting to produce the final string.

## Pipeline Components
- [ai-service/src/alpr/pipeline.py](ai-service/src/alpr/pipeline.py)
  - Coordinates detection + recognition for each frame.
- [ai-service/src/alpr/models/yolo_detector.py](ai-service/src/alpr/models/yolo_detector.py)
  - Plate detection wrapper.
- [ai-service/src/alpr/models/yolo_char_recognizer.py](ai-service/src/alpr/models/yolo_char_recognizer.py)
  - Character detection wrapper + decoding.

## Configuration
- Inference config: [ai-service/configs/pipeline.yaml](ai-service/configs/pipeline.yaml)
  - Model paths, charset, device, and thresholds.
- Batch pipeline runner: [ai-service/configs/pipeline_run.yaml](ai-service/configs/pipeline_run.yaml)
  - Lists the scripts to run in order.

## CARLA Integration
- [ai-service/src/alpr/carla_bridge.py](ai-service/src/alpr/carla_bridge.py)
  - Convert CARLA frames to OpenCV images and pass them into the pipeline.
  - Configure camera IDs in [ai-service/configs/carla.yaml](ai-service/configs/carla.yaml).

## Backend Publishing
- [ai-service/src/alpr/runtime.py](ai-service/src/alpr/runtime.py)
  - Serializes recognized plate results and posts them to a backend endpoint.
- `--backend-url`
  - Enables HTTP publishing from the CLI entrypoint.

## Local Testing
- `--self-test`
  - Runs a deterministic in-memory detector and recognizer so you can validate the
    orchestration without CARLA, model files, or backend connectivity.

## Typical Flow
1. Prepare detection data.
2. Train plate detector.
3. Prepare recognition data.
4. Train character recognizer.
5. Run inference on images or CARLA streams.
