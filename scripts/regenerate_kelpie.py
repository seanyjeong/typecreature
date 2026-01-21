#!/usr/bin/env python3
"""켈피(46번) 이미지 재생성 - 뒷다리 대신 물고기 꼬리"""

import os
import requests
from pathlib import Path

API_KEY = os.environ.get("OPENAI_API_KEY")
API_URL = "https://api.openai.com/v1/images/generations"

BASE_STYLE = "cute pixel art, 64x64 pixels, pastel colors, big eyes, round shape, transparent background, game asset, tamagotchi style, adorable, simple design, white background"

# 켈피: 앞다리 + 물고기 꼬리 (뒷다리 없음)
KELPIE_PROMPT = "kelpie water horse, front legs only, fish tail instead of back legs, mermaid horse, blue-green colors, seaweed mane, water drops, mystical, hippocampus style, no hind legs"

def main():
    if not API_KEY:
        print("OPENAI_API_KEY 환경변수를 설정해주세요")
        return

    output_path = Path(__file__).parent.parent / "TypingTamagotchi" / "Assets" / "Creatures" / "46.png"

    # 백업
    if output_path.exists():
        backup_path = output_path.with_suffix('.png.bak')
        output_path.rename(backup_path)
        print(f"기존 이미지 백업: {backup_path}")

    print("켈피 이미지 생성 중...")

    headers = {
        "Authorization": f"Bearer {API_KEY}",
        "Content-Type": "application/json"
    }

    data = {
        "model": "dall-e-3",
        "prompt": f"{BASE_STYLE}, {KELPIE_PROMPT}",
        "n": 1,
        "size": "1024x1024",
        "quality": "standard",
        "response_format": "url"
    }

    try:
        response = requests.post(API_URL, headers=headers, json=data, timeout=60)

        if response.status_code != 200:
            print(f"API 에러: {response.json()}")
            return

        result = response.json()
        image_url = result["data"][0]["url"]

        # 이미지 다운로드
        img_response = requests.get(image_url, timeout=30)
        img_response.raise_for_status()

        with open(output_path, "wb") as f:
            f.write(img_response.content)

        print(f"완료! {output_path}")

    except Exception as e:
        print(f"에러: {e}")

if __name__ == "__main__":
    main()
