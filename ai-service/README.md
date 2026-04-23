# AI Service (ALPR)

This service downloads the EALPR dataset, prepares training splits, and runs ALPR inference
for multiple cameras (e.g., CARLA simulator).

## Quick Start

1. Place dataset
2. Run the full pipeline from YAML
   - `uv run python scripts/run_pipeline.py --config configs/pipeline_run.yaml`
3. Run inference
   - `uv run python main.py --config configs/pipeline.yaml --image path/to/image.jpg`
4. Run the built-in self-test without CARLA or backend
   - `uv run python main.py --self-test`
5. Run local integration test (no real backend required)
   - `uv run python main.py --integration-test`
6. Run live from CARLA and forward recognized plates to backend
   - `uv run python main.py --config configs/pipeline.yaml --carla --backend-url http://localhost:5000/api/plates`

To run only some steps:
- `uv run python scripts/run_pipeline.py --only prepare_detection,train_detector`
- `uv run python scripts/run_pipeline.py --skip train_recognizer`

## Notes
- Trained artifacts are auto-resolved from config path and common artifact locations.
- If `configs/pipeline.yaml` uses relative model paths, the service tries these locations:
   - config-relative path
   - `ai-service/artifacts/...`
   - `ai-service/artifacts/models/plate-detector.pt`
   - `ai-service/artifacts/models/plate-characters.pt`
- Use `--backend-url` to POST recognized plate events to your backend.
- For CARLA mode, make sure CARLA is running and has at least one vehicle actor.
- Optional CARLA flags: `--carla-host`, `--carla-port`, `--carla-vehicle-id`,
   `--carla-camera-id`, `--carla-duration`.

## Architecture
- See [ai-service/ARCHITECTURE.md](ai-service/ARCHITECTURE.md) for system flow and model details.
