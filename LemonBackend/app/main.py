from fastapi import FastAPI
from app.api.dialogues import router as dialogues_router

app = FastAPI(title="Lemon Empire API")

# Подключаем наши роутеры
app.include_router(dialogues_router, prefix="/api/v1/dialogues", tags=["Dialogues"])

@app.get("/")
async def root():
    return {"message": "Lemon Empire Server is running!"}