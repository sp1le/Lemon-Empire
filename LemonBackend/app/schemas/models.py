from pydantic import BaseModel
from typing import List

# То, что присылает Unity (Входящий DTO)
class PlayerStateRequest(BaseModel):
    reaction_score: int
    morale: int
    outfit: str
    state: str
    # Для мульти-сообщений
    dialogue_step: int = 1
    player_reply: str = None
    haggle_discount: int = 0

# Структура ответа (Исходящий DTO)
class DialogOption(BaseModel):
    id: int = 0
    text: str
    type: str = "normal"  # "normal", "desperate", "bargain_2_for_1", "agree_discount"

class NpcResponse(BaseModel):
    npc_greeting: str
    player_options: List[DialogOption]