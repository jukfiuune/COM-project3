# This is run on a seperate, way more powerful server (due to the model choice)

import base64
import io
import time
import logging
from contextlib import asynccontextmanager

import cv2
import numpy as np
from fastapi import FastAPI, File, UploadFile, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from PIL import Image
from ultralytics import YOLO

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(name)s: %(message)s")
logger = logging.getLogger(__name__)

_state = {"model": None, "status": "loading"}
CONF_THRESHOLD = 0.25

TACO_CLASSES = {
    0: 'Aluminum foil', 1: 'Battery', 2: 'Blister pack', 3: 'Bottle', 4: 'Bottle cap', 
    5: 'Broken glass', 6: 'Can', 7: 'Carton', 8: 'Cigarette', 9: 'Cup', 
    10: 'Food waste', 11: 'Glass jar', 12: 'Other plastic', 13: 'Paper', 
    14: 'Paper bag', 15: 'Plastic bag & wrapper', 16: 'Plastic container', 
    17: 'Plastic Gloves', 18: 'Plastic utensils', 19: 'Pop tab', 20: 'Rope & strings', 
    21: 'Scrap metal', 22: 'Shoe', 23: 'Squeezable tube', 24: 'Straw', 
    25: 'Styrofoam piece', 26: 'Unlabeled litter', 27: 'Other'
}

@asynccontextmanager
async def lifespan(app: FastAPI):
    try:
        _state["model"] = YOLO("yolo26x.pt")
        _state["status"] = "ready"
        logger.info("Model ready")
    except Exception as exc:
        logger.exception(f"Failed to load model: {exc}")
        _state["status"] = "error"
    yield
    logger.info("Shutting down")

app = FastAPI(title="Garbage AI", lifespan=lifespan)
app.add_middleware(CORSMiddleware, allow_origins=["*"], allow_methods=["*"], allow_headers=["*"])

@app.get("/health")
def health():
    return {"status": _state["status"], "classes": TACO_CLASSES}

@app.post("/detect")
async def detect(file: UploadFile = File(...)):
    if _state["status"] != "ready":
        raise HTTPException(status_code=503, detail="Model not ready yet.")

    raw = await file.read()
    try:
        pil_img = Image.open(io.BytesIO(raw)).convert("RGB")
    except Exception:
        raise HTTPException(status_code=400, detail="Could not decode image.")

    img_np = np.array(pil_img)
    img_bgr = cv2.cvtColor(img_np, cv2.COLOR_RGB2BGR)

    t0 = time.perf_counter()
    results = _state["model"](img_np, conf=CONF_THRESHOLD, verbose=False)[0]
    inference_ms = round((time.perf_counter() - t0) * 1000, 1)

    detections = []
    annotated = img_bgr.copy()

    for box in results.boxes:
        cls_id = int(box.cls[0])
        conf = float(box.conf[0])
        label = TACO_CLASSES.get(cls_id, f"class_{cls_id}")
        x1, y1, x2, y2 = map(int, box.xyxy[0].tolist())

        color = (255, 0, 0)
        cv2.rectangle(annotated, (x1, y1), (x2, y2), color, 2)
        text = f"{label} {conf:.0%}"
        (tw, th), _ = cv2.getTextSize(text, cv2.FONT_HERSHEY_SIMPLEX, 0.55, 1)
        cv2.rectangle(annotated, (x1, y1 - th - 8), (x1 + tw + 4, y1), color, -1)
        cv2.putText(annotated, text, (x1 + 2, y1 - 4), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (255, 255, 255), 1, cv2.LINE_AA)

        detections.append({
            "label": label,
            "confidence": round(conf, 4),
            "bbox": {"x1": x1, "y1": y1, "x2": x2, "y2": y2},
            "cls_id": cls_id
        })

    _, buf = cv2.imencode(".png", annotated)
    img_b64 = base64.b64encode(buf).decode("utf-8")

    return {
        "inference_ms": inference_ms,
        "detections": detections,
        "annotated_image": img_b64,
    }

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=False)

