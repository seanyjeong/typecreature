#!/usr/bin/env python3
"""레전더리 알 이미지 생성"""

import os
import base64
import requests
from pathlib import Path
from rembg import remove

API_KEY = os.environ.get("GEMINI_API_KEY")
API_URL = "https://generativelanguage.googleapis.com/v1beta/models/imagen-4.0-fast-generate-001:predict"

OUTPUT_PATH = Path(__file__).parent.parent / "TypingTamagotchi" / "Assets" / "Eggs" / "전설알.png"

PROMPT = """A single egg object only, no animals no creatures,
pixel art style egg shape, 64x64 pixels,
BRIGHT GOLD colored egg shell, shiny metallic golden surface,
golden crown on top of the egg, golden sparkle stars around,
royal legendary precious egg, diamond gem decorations,
luxury golden treasure egg, yellow gold color,
simple game asset, white background, cute tamagotchi style"""

def main():
    if not API_KEY:
        print("GEMINI_API_KEY 환경변수를 설정해주세요")
        return

    print("레전더리 알 이미지 생성 중...")

    headers = {
        "x-goog-api-key": API_KEY,
        "Content-Type": "application/json"
    }

    data = {
        "instances": [{"prompt": PROMPT}],
        "parameters": {
            "sampleCount": 1,
            "aspectRatio": "1:1"
        }
    }

    try:
        response = requests.post(API_URL, headers=headers, json=data, timeout=120)

        if response.status_code != 200:
            print(f"API 에러: {response.text[:200]}")
            return

        result = response.json()
        if "predictions" not in result:
            print("예측 결과 없음")
            return

        img_b64 = result["predictions"][0].get("bytesBase64Encoded")
        if not img_b64:
            print("이미지 데이터 없음")
            return

        raw_img = base64.b64decode(img_b64)

        # 배경 제거
        print("배경 제거 중...")
        transparent_img = remove(raw_img)

        # 저장
        with open(OUTPUT_PATH, "wb") as f:
            f.write(transparent_img)

        print(f"완료! {OUTPUT_PATH}")

    except Exception as e:
        print(f"에러: {e}")

if __name__ == "__main__":
    main()
