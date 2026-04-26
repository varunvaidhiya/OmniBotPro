import time
import uvicorn
from fastapi import FastAPI, HTTPException
from contextlib import asynccontextmanager
from ..models.openvla import OpenVLAModel
from ..utils.image import decode_base64_image
from .schema import InferenceRequest, InferenceResponse

# Global model instance
model_instance = None


@asynccontextmanager
async def lifespan(app: FastAPI):
    # Load model on startup
    global model_instance
    print("Initializing VLA Engine Inference Server...")
    try:
        model_instance = OpenVLAModel()
        # In production, make the path configurable via env vars
        # For now, we lazily load or load a small dummy/base model if desired
        # To avoid massive download on first run without checking, we might want to defer or use a specific flag
        print(
            "Model instance created. Waiting for explicit load or auto-loading if configured."
        )
        # model_instance.load_model() # Uncomment to load on startup
    except Exception as e:
        print(f"Error initializing model: {e}")

    yield

    # Clean up
    if model_instance:
        del model_instance


app = FastAPI(lifespan=lifespan)


@app.get("/health")
def health_check():
    return {
        "status": "ok",
        "model_loaded": model_instance.model is not None if model_instance else False,
    }


@app.post("/load_model")
def load_model(model_path: str = "openvla/openvla-7b", load_4bit: bool = False):
    global model_instance
    if not model_instance:
        model_instance = OpenVLAModel()

    try:
        model_instance.load_model(model_path=model_path, load_in_4bit=load_4bit)
        return {"status": "success", "message": f"Loaded {model_path}"}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/predict", response_model=InferenceResponse)
def predict(request: InferenceRequest):
    global model_instance
    if not model_instance or not model_instance.model:
        raise HTTPException(
            status_code=503, detail="Model not loaded. Call /load_model first."
        )

    try:
        start_time = time.time()

        # Decode image
        image = decode_base64_image(request.image_base64)

        # Run inference
        result = model_instance.predict_action(image, request.instruction)

        end_time = time.time()
        latency = (end_time - start_time) * 1000

        return InferenceResponse(
            action=result if isinstance(result, dict) else {"vector": result},
            raw_output=str(result),
            latency_ms=latency,
        )

    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8000)
