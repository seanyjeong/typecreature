#!/usr/bin/env python3
"""
TypeCreature 픽셀아트 에셋 생성 스크립트
DALL-E API를 사용하여 50종 크리처 + 5종 알 이미지 생성
"""

import os
import sys
import time
import requests
from pathlib import Path

# OpenAI API 설정
API_KEY = os.environ.get("OPENAI_API_KEY")
API_URL = "https://api.openai.com/v1/images/generations"

# 기본 스타일
BASE_STYLE = "cute pixel art, 64x64 pixels, pastel colors, big eyes, round shape, transparent background, game asset, tamagotchi style, adorable, simple design, white background"

# 크리처 목록 (이름, 프롬프트)
CREATURES = [
    # Legendary (3)
    ("황금드래곤", "golden baby dragon, golden scales, small wings, sparkly, legendary aura, glowing"),
    ("세계수정령", "tree spirit, green and brown, leaf crown, nature essence, magical particles, forest guardian"),
    ("시간고양이", "cosmic cat, galaxy pattern fur, clock motifs, mysterious, floating stars, purple and blue"),

    # Epic (7)
    ("용아기", "baby dragon, red scales, tiny wings, breathing small flame, innocent"),
    ("유니콘", "unicorn, white body, rainbow mane, golden horn, sparkles, magical"),
    ("피닉스", "baby phoenix, orange and red feathers, small flames, golden eyes"),
    ("크라켄", "baby kraken, purple tentacles, big round eyes, underwater, bubbles"),
    ("그리폰", "baby griffin, eagle head, lion body, small wings, golden feathers"),
    ("켈피", "water horse, blue-green colors, seaweed mane, water drops, mystical"),
    ("바실리스크", "baby basilisk, green scales, crown pattern, yellow eyes, not scary"),

    # Rare (15)
    ("번개토끼", "electric bunny, yellow fur, lightning bolt ears, sparks"),
    ("불꽃여우", "fire fox, orange fur, flame tail, warm colors"),
    ("얼음펭귄", "ice penguin, blue and white, ice crystals, winter scarf"),
    ("바람새", "wind bird, light blue feathers, wind swirls, floating"),
    ("꽃사슴", "flower deer, brown body, flower antlers, pink blossoms"),
    ("달토끼", "moon bunny, white fur, crescent moon, glowing, night"),
    ("무지개뱀", "rainbow snake, colorful scales, rainbow pattern, friendly"),
    ("구름고래", "cloud whale, white fluffy body, floating, sky blue, dreamy"),
    ("수정나비", "crystal butterfly, transparent wings, sparkles, magical"),
    ("숲요정", "forest fairy, green outfit, leaf wings, tiny, nature"),
    ("별똥곰", "star bear, dark blue fur, star patterns, glowing, night sky"),
    ("파도물개", "wave seal, blue-gray body, water splash, ocean"),
    ("안개늑대", "mist wolf, gray fur, misty aura, mysterious, ethereal"),
    ("노을새", "sunset bird, orange and pink feathers, warm glow, evening"),
    ("이끼거북", "moss turtle, green shell with plants, flowers on back, garden"),

    # Common (25)
    ("슬라임", "slime, translucent green, jiggly, simple, classic"),
    ("꼬마구름", "tiny cloud, white fluffy, floating, happy face"),
    ("잎새", "leaf creature, green leaf body, floating"),
    ("물방울", "water drop, blue translucent, shiny"),
    ("돌멩이", "rock creature, gray stone, round"),
    ("별똥별", "tiny star, yellow glowing, trail"),
    ("꽃잎", "flower petal, pink petal body, floating"),
    ("솜뭉치", "cotton ball, white fluffy, round, soft"),
    ("젤리콩", "jelly bean, colorful, translucent, bouncy"),
    ("이끼돌", "mossy rock, gray with green moss"),
    ("눈송이", "snowflake, white crystalline, sparkling"),
    ("반딧불", "firefly, yellow glow, tiny wings, night"),
    ("씨앗", "seed, brown oval, tiny sprout"),
    ("조약돌", "pebble, smooth gray, round"),
    ("먼지토끼", "dust bunny, gray fluffy, round, floating"),
    ("비누방울", "soap bubble, rainbow iridescent, floating"),
    ("도토리", "acorn, brown cap, tan body"),
    ("꿀방울", "honey drop, golden amber, shiny"),
    ("깃털", "feather, white soft, floating"),
    ("이슬", "dew drop, clear sparkly, on leaf"),
    ("모래알", "sand grain, beige tiny, beach"),
    ("풀잎", "grass blade, green simple, swaying"),
    ("나뭇가지", "twig, brown stick, tiny leaves"),
    ("진흙이", "mud blob, brown squishy"),
    ("버섯", "mushroom, red cap white spots"),
]

# 알 목록
EGGS = [
    ("불꽃알", "fire egg, orange with flame pattern, glowing, warm"),
    ("물방울알", "water egg, blue with water drops, translucent, cool"),
    ("바람알", "wind egg, light green with swirl pattern, airy"),
    ("대지알", "earth egg, brown with crystal pattern, solid"),
    ("번개알", "lightning egg, yellow with electric pattern, sparking"),
]


def generate_image(prompt: str, filename: str, output_dir: Path) -> bool:
    """DALL-E API로 이미지 생성"""
    full_prompt = f"{BASE_STYLE}, {prompt}"

    headers = {
        "Authorization": f"Bearer {API_KEY}",
        "Content-Type": "application/json"
    }

    data = {
        "model": "dall-e-3",
        "prompt": full_prompt,
        "n": 1,
        "size": "1024x1024",
        "quality": "standard",
        "response_format": "url"
    }

    try:
        response = requests.post(API_URL, headers=headers, json=data, timeout=60)

        if response.status_code != 200:
            error_detail = response.json() if response.text else {"error": "No response body"}
            print(f"  API 에러 ({response.status_code}): {error_detail}")
            return False

        result = response.json()
        image_url = result["data"][0]["url"]

        # 이미지 다운로드
        img_response = requests.get(image_url, timeout=30)
        img_response.raise_for_status()

        output_path = output_dir / filename
        with open(output_path, "wb") as f:
            f.write(img_response.content)

        return True

    except Exception as e:
        print(f"  에러: {e}")
        return False


def main():
    if not API_KEY:
        print("OPENAI_API_KEY 환경변수를 설정해주세요")
        sys.exit(1)

    # 출력 디렉토리 생성
    base_dir = Path(__file__).parent.parent / "TypingTamagotchi" / "Assets"
    creatures_dir = base_dir / "Creatures"
    eggs_dir = base_dir / "Eggs"

    creatures_dir.mkdir(parents=True, exist_ok=True)
    eggs_dir.mkdir(parents=True, exist_ok=True)

    print("=" * 50)
    print("TypeCreature 픽셀아트 생성 시작")
    print("=" * 50)

    # 크리처 생성 (50개)
    print(f"\n크리처 생성 중... (총 {len(CREATURES)}개)")
    for i, (name, prompt) in enumerate(CREATURES, 1):
        filename = f"{i}.png"
        existing = creatures_dir / filename

        if existing.exists():
            print(f"[{i:02d}/50] {name} - 이미 존재함, 스킵")
            continue

        print(f"[{i:02d}/50] {name} 생성 중...", end=" ", flush=True)

        if generate_image(prompt, filename, creatures_dir):
            print("완료!")
        else:
            print("실패")

        # Rate limit 방지
        time.sleep(1)

    # 알 생성 (5개)
    print(f"\n알 생성 중... (총 {len(EGGS)}개)")
    for i, (name, prompt) in enumerate(EGGS, 1):
        filename = f"{name}.png"
        existing = eggs_dir / filename

        if existing.exists():
            print(f"[{i}/5] {name} - 이미 존재함, 스킵")
            continue

        print(f"[{i}/5] {name} 생성 중...", end=" ", flush=True)

        if generate_image(prompt, filename, eggs_dir):
            print("완료!")
        else:
            print("실패")

        time.sleep(1)

    print("\n" + "=" * 50)
    print("생성 완료!")
    print(f"크리처: {creatures_dir}")
    print(f"알: {eggs_dir}")
    print("=" * 50)


if __name__ == "__main__":
    main()
