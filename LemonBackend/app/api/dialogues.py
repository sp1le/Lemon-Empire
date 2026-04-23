from fastapi import APIRouter
from app.schemas.models import PlayerStateRequest, NpcResponse
from app.services.llm import generate_dialogue

router = APIRouter()

@router.post("/generate", response_model=NpcResponse)
async def create_dialogue(request: PlayerStateRequest):
    # Принимаем запрос, отдаем в сервис, возвращаем результат
    response = await generate_dialogue(request)
    return response