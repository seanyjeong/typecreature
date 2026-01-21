#!/usr/bin/env python3
"""켈피(46번) 이미지 재생성 - 앞다리 + 물고기 꼬리"""

import base64
import requests
from pathlib import Path
from rembg import remove

import os
API_KEY = os.environ.get("GEMINI_API_KEY")
API_URL = "https://generativelanguage.googleapis.com/v1beta/models/imagen-4.0-fast-generate-001:predict"

# 켈피 프롬프트 - 앞다리만 있고 뒷다리 대신 물고기 꼬리
KELPIE_PROMPT = """cute pixel art hippocampus sea horse creature, 64x64 pixels,
ONLY TWO FRONT LEGS, NO BACK LEGS AT ALL,
lower body is a big curved fish tail like a mermaid,
horse head and front body, fish tail lower body,
light cream colored belly, blue-green scales on tail,
green seaweed flowing mane, water theme,
simple solid colors, no transparency inside the creature,
game asset, white background, tamagotchi kawaii style, big cute eyes"""

OUTPUT_PATH = Path(__file__).parent.parent / "TypingTamagotchi" / "Assets" / "Creatures" / "46.png"

def main():
    print("켈피 이미지 재생성 중...")
    print(f"프롬프트: {KELPIE_PROMPT[:100]}...")

    # 백업
    if OUTPUT_PATH.exists():
        backup = OUTPUT_PATH.with_suffix('.png.bak')
        OUTPUT_PATH.rename(backup)
        print(f"기존 이미지 백업: {backup}")

    headers = {
        "x-goog-api-key": API_KEY,
        "Content-Type": "application/json"
    }

    data = {
        "instances": [{"prompt": KELPIE_PROMPT}],
        "parameters": {
            "sampleCount": 1,
            "aspectRatio": "1:1"
        }
    }

    try:
        response = requests.post(API_URL, headers=headers, json=data, timeout=120)

        if response.status_code != 200:
            print(f"API 에러: {response.text[:300]}")
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
