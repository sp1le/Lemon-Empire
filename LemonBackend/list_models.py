import os
from openai import OpenAI
from dotenv import load_dotenv

load_dotenv()

client = OpenAI(
    base_url=os.getenv('LLM_BASE_URL', 'https://generativelanguage.googleapis.com/v1beta/openai/'),
    api_key=os.getenv('GEMINI_API_KEY')
)

try:
    models = client.models.list()
    with open("available_models.txt", "w", encoding="utf-8") as f:
        for m in models.data:
            f.write(f"{m.id}\n")
    print("Available model IDs written to available_models.txt")
except Exception as e:
    print(f"Error: {e}")
