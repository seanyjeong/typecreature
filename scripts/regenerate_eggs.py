#!/usr/bin/env python3
"""알 이미지 5개 재생성"""

import os
import base64
import requests
import time
from pathlib import Path
from rembg import remove

API_KEY = os.environ.get("GEMINI_API_KEY")
API_URL = "https://generativelanguage.googleapis.com/v1beta/models/imagen-4.0-fast-generate-001:predict"

OUTPUT_DIR = Path(__file__).parent.parent / "TypingTamagotchi" / "Assets" / "Eggs"

# 알 데이터: (파일명, 프롬프트)
EGGS = [
    ("불꽃알.png", """A single egg object only, no animals no creatures,
pixel art style egg shape, 64x64 pixels,
orange and red colored egg shell with flame pattern decorations,
fire element theme egg, warm colors,
simple game asset, white background, cute tamagotchi style"""),

    ("물방울알.png", """A single egg object only, no animals no creatures,
pixel art style egg shape, 64x64 pixels,
blue colored egg shell with water bubble pattern decorations,
water element theme egg, ocean blue colors,
simple game asset, white background, cute tamagotchi style"""),

    ("바람알.png", """A single egg object only, no animals no creatures,
pixel art style egg shape, 64x64 pixels,
light green colored egg shell with swirl wind pattern decorations,
wind element theme egg, mint green colors,
simple game asset, white background, cute tamagotchi style"""),
]

def generate_egg(filename: str, prompt: str) -> bool:
    """단일 알 이미지 생성"""
    output_path = OUTPUT_DIR / filename

    # 백업
    if output_path.exists():
        backup = output_path.with_suffix('.png.bak')
        output_path.rename(backup)
        print(f"  백업: {backup.name}")

    headers = {
        "x-goog-api-key": API_KEY,
        "Content-Type": "application/json"
    }

    data = {
        "instances": [{"prompt": prompt}],
        "parameters": {
            "sampleCount": 1,
            "aspectRatio": "1:1"
        }
    }

    try:
        response = requests.post(API_URL, headers=headers, json=data, timeout=120)

        if response.status_code != 200:
            print(f"  API 에러: {response.text[:200]}")
            return False

        result = response.json()
        if "predictions" not in result:
            print("  예측 결과 없음")
            return False

        img_b64 = result["predictions"][0].get("bytesBase64Encoded")
        if not img_b64:
            print("  이미지 데이터 없음")
            return False

        raw_img = base64.b64decode(img_b64)

        # 배경 제거
        transparent_img = remove(raw_img)

        # 저장
        with open(output_path, "wb") as f:
            f.write(transparent_img)

        return True

    except Exception as e:
        print(f"  에러: {e}")
        return False

def main():
    if not API_KEY:
        print("GEMINI_API_KEY 환경변수를 설정해주세요")
        return

    print("알 이미지 5개 재생성 시작...\n")

    for i, (filename, prompt) in enumerate(EGGS, 1):
        print(f"[{i}/5] {filename} 생성 중...")

        if generate_egg(filename, prompt):
            print(f"  ✅ 완료!")
        else:
            print(f"  ❌ 실패")

        # API 레이트 리밋 방지
        if i < len(EGGS):
            time.sleep(2)

    print("\n모든 알 이미지 생성 완료!")

if __name__ == "__main__":
    main()
