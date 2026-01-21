#!/usr/bin/env python3
"""
놀이터 에셋 생성 스크립트
Imagen 4.0 API + rembg 배경 제거
"""

import os
import base64
import requests
from pathlib import Path
from rembg import remove

# Gemini API 설정
API_KEY = os.environ.get("GEMINI_API_KEY")
API_URL = "https://generativelanguage.googleapis.com/v1beta/models/imagen-4.0-fast-generate-001:predict"

# 경로 설정
BASE_DIR = Path(__file__).parent.parent / "TypingTamagotchi" / "Assets" / "Playground"

# 에셋 데이터
ASSETS = [
    (
        "background",
        "pixel art simple grassy field background only, 400x150 pixels wide horizontal format, "
        "green grass texture at bottom, light blue gradient sky, small white clouds, "
        "tiny colorful flowers in grass, NO animals, NO characters, NO creatures, "
        "empty landscape scene, game background asset, clean minimal style"
    ),
    (
        "bump_effect",
        "cute pixel art comic impact effect, 64x64 pixels, bold text 'BUMP!' in comic style, "
        "yellow and orange starburst explosion behind text, action lines radiating outward, "
        "cartoon collision effect, game asset, transparent background, kawaii style"
    ),
]


def generate_and_save(name: str, prompt: str, remove_bg: bool = False) -> bool:
    """이미지 생성 + 저장 (배경 제거 선택적)"""
    headers = {
        "x-goog-api-key": API_KEY,
        "Content-Type": "application/json"
    }

    data = {
        "instances": [{"prompt": prompt}],
        "parameters": {
            "sampleCount": 1,
            "aspectRatio": "1:1" if name == "bump_effect" else "16:9"
        }
    }

    output_path = BASE_DIR / f"{name}.png"

    try:
        print(f"[{name}] 생성 중...")
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

        # 배경 제거 (bump_effect만)
        if remove_bg:
            raw_img = remove(raw_img)

        # 저장
        output_path.parent.mkdir(parents=True, exist_ok=True)
        with open(output_path, "wb") as f:
            f.write(raw_img)

        print(f"  ✅ 저장됨: {output_path}")
        return True

    except Exception as e:
        print(f"  ❌ 에러: {e}")
        return False


if __name__ == "__main__":
    import sys

    if not API_KEY:
        print("GEMINI_API_KEY 환경변수를 설정하세요")
        exit(1)

    print("=== 놀이터 에셋 생성 ===\n")

    # 인자로 특정 에셋만 생성 가능
    target = sys.argv[1] if len(sys.argv) > 1 else "all"

    if target in ["all", "background"]:
        generate_and_save("background", ASSETS[0][1], remove_bg=False)

    if target in ["all", "bump"]:
        generate_and_save("bump_effect", ASSETS[1][1], remove_bg=True)

    print("\n완료!")
