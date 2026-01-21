#!/usr/bin/env python3
"""배경 여러 개 생성해서 고르기"""

import os
import base64
import requests
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor, as_completed

API_KEY = os.environ.get("GEMINI_API_KEY")
API_URL = "https://generativelanguage.googleapis.com/v1beta/models/imagen-4.0-fast-generate-001:predict"
BASE_DIR = Path(__file__).parent.parent / "TypingTamagotchi" / "Assets" / "Playground"

PROMPT = (
    "simple pixel art grass field background, wide horizontal banner format, "
    "flat green grass ground at bottom third, plain light blue sky gradient, "
    "few small white clouds, scattered tiny flowers on grass, "
    "NO trees, NO animals, NO characters, NO creatures, NO people, "
    "empty minimal clean background, retro game style, 8-bit aesthetic"
)

def generate_one(index: int) -> bool:
    headers = {
        "x-goog-api-key": API_KEY,
        "Content-Type": "application/json"
    }
    data = {
        "instances": [{"prompt": PROMPT}],
        "parameters": {"sampleCount": 1, "aspectRatio": "16:9"}
    }

    output_path = BASE_DIR / f"background_{index}.png"

    try:
        response = requests.post(API_URL, headers=headers, json=data, timeout=120)
        if response.status_code != 200:
            print(f"  [{index}] API 에러")
            return False

        result = response.json()
        if "predictions" not in result:
            return False

        img_b64 = result["predictions"][0].get("bytesBase64Encoded")
        if not img_b64:
            return False

        raw_img = base64.b64decode(img_b64)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        with open(output_path, "wb") as f:
            f.write(raw_img)

        print(f"  ✅ [{index}] 저장됨")
        return True
    except Exception as e:
        print(f"  ❌ [{index}] 에러: {e}")
        return False

if __name__ == "__main__":
    print("=== 배경 5개 병렬 생성 ===\n")

    with ThreadPoolExecutor(max_workers=5) as executor:
        futures = {executor.submit(generate_one, i): i for i in range(1, 6)}
        for future in as_completed(futures):
            pass

    print("\n완료! background_1.png ~ background_5.png 확인하세요")
