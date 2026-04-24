"""
FastAPI server exposing the ALPR pipeline as an HTTP endpoint.

Usage:
    uv run uvicorn api:app --host 0.0.0.0 --port 8001
    # or
    python api.py
"""
from __future__ import annotations

import io
import sys
from pathlib import Path

import cv2
import numpy as np
import uvicorn
from fastapi import FastAPI, File, UploadFile
from fastapi.responses import JSONResponse

ROOT = Path(__file__).resolve().parent
sys.path.append(str(ROOT / "src"))

app = FastAPI(title="ALPR Service", version="1.0.0")

# Global pipeline — initialized on first request or at startup
_pipeline = None


def _get_pipeline():
    global _pipeline
    if _pipeline is not None:
        return _pipeline

    from main import build_pipeline
    config_path = ROOT / "configs" / "pipeline.yaml"
    _pipeline = build_pipeline(config_path)
    return _pipeline


@app.on_event("startup")
async def startup_event():
    """Try to load pipeline on startup; fall back to lazy loading."""
    try:
        _get_pipeline()
        print("✅ ALPR pipeline loaded successfully")
    except Exception as e:
        print(f"⚠️  Pipeline not loaded at startup (will retry on first request): {e}")


@app.post("/recognize")
async def recognize_plate(file: UploadFile = File(...)):
    """
    Accepts an image file and returns detected license plates.

    Returns:
        { "plates": [{ "plate_text": "...", "score": 0.95, "bbox": [x1,y1,x2,y2] }] }
    """
    contents = await file.read()
    nparr = np.frombuffer(contents, np.uint8)
    image = cv2.imdecode(nparr, cv2.IMREAD_COLOR)

    if image is None:
        return JSONResponse(
            status_code=400,
            content={"error": "Could not decode image"}
        )

    pipeline = _get_pipeline()
    results = pipeline.process_frame(image)

    plates = []
    print(f"\n[DEBUG] Pipeline ran. Found {len(results)} potential plates.")
    for result in results:
        print(f"  - Detected Text: '{result.text}' | Score: {result.score:.3f} | BBox: {result.bbox}")
        plates.append({
            "plate_text": result.text,
            "score": float(result.score),
            "bbox": list(result.bbox),
        })

    return {"plates": plates}


@app.get("/health")
async def health():
    return {"status": "ok"}


if __name__ == "__main__":
    uvicorn.run("api:app", host="0.0.0.0", port=8001, reload=False)
