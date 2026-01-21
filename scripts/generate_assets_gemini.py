#!/usr/bin/env python3
"""
TypeCreature 픽셀아트 에셋 생성 스크립트
Imagen 4.0 API + rembg 배경 제거
"""

import os
import sys
import time
import base64
import requests
from pathlib import Path
from rembg import remove

# Gemini API 설정
API_KEY = os.environ.get("GEMINI_API_KEY")
API_URL = "https://generativelanguage.googleapis.com/v1beta/models/imagen-4.0-fast-generate-001:predict"

# 경로 설정
BASE_DIR = Path(__file__).parent.parent / "TypingTamagotchi" / "Assets"
CREATURES_DIR = BASE_DIR / "Creatures"
EGGS_DIR = BASE_DIR / "Eggs"

# 크리처 데이터 (번호, 이름, 영문파일명, 프롬프트)
CREATURES = [
    # Legendary (1-3)
    (1, "황금드래곤", "golden_dragon", "cute pixel art golden baby dragon, 64x64 pixels, golden scales, small wings, big sparkly eyes, glowing golden border, sparkling glitter effect, mysterious legendary aura, game asset, transparent background, tamagotchi style"),
    (2, "세계수정령", "world_tree_spirit", "cute pixel art ancient tree spirit, 64x64 pixels, lush greenery, glowing golden border, nature essence, magical floating leaves, mysterious aura, game asset, transparent background, tamagotchi style"),
    (3, "시간고양이", "time_cat", "cute pixel art cosmic cat, 64x64 pixels, galaxy pattern fur, clock motifs, glowing golden border, floating star particles, mysterious aura, game asset, transparent background, tamagotchi style"),

    # Epic (4-10)
    (4, "용아기", "baby_dragon", "cute pixel art baby dragon, 64x64 pixels, purple and pink fire effects, flashy magical aura, big innocent eyes, game asset, transparent background, tamagotchi style"),
    (5, "유니콘", "unicorn", "cute pixel art unicorn, 64x64 pixels, rainbow mane, golden horn, purple sparkling dust, flashy magical trail, game asset, transparent background, tamagotchi style"),
    (6, "피닉스", "phoenix", "cute pixel art baby phoenix, 64x64 pixels, glowing purple flames, pink feather accents, flashy fire particles, game asset, transparent background, tamagotchi style"),
    (7, "크라켄", "kraken", "cute pixel art baby kraken, 64x64 pixels, purple glowing tentacles, pink bubble effects, flashy underwater aura, game asset, transparent background, tamagotchi style"),
    (8, "그리폰", "griffin", "cute pixel art baby griffin, 64x64 pixels, purple magical wings, pink glow, flashy stardust effect, game asset, transparent background, tamagotchi style"),
    (9, "켈피", "kelpie", "cute pixel art water horse, 64x64 pixels, purple misty mane, pink water droplets, flashy mystical aura, game asset, transparent background, tamagotchi style"),
    (10, "바실리스크", "basilisk", "cute pixel art baby basilisk, 64x64 pixels, purple scale patterns, pink glowing eyes, flashy toxic mist effect, game asset, transparent background, tamagotchi style"),

    # Rare (11-25)
    (11, "번개토끼", "lightning_rabbit", "cute pixel art electric bunny, 64x64 pixels, yellow fur, blue lightning sparks, subtle electric glow, game asset, transparent background, tamagotchi style"),
    (12, "불꽃여우", "fire_fox", "cute pixel art fire fox, 64x64 pixels, orange fur, blue flame tip, subtle heat haze effect, game asset, transparent background, tamagotchi style"),
    (13, "얼음펭귄", "ice_penguin", "cute pixel art ice penguin, 64x64 pixels, blue ice crystals, subtle snow frost effect, winter theme, game asset, transparent background, tamagotchi style"),
    (14, "바람새", "wind_bird", "cute pixel art wind bird, 64x64 pixels, light blue feathers, subtle wind swirls, floating, game asset, transparent background, tamagotchi style"),
    (15, "꽃사슴", "flower_deer", "cute pixel art flower deer, 64x64 pixels, blue flower antlers, subtle petal wind, spring theme, game asset, transparent background, tamagotchi style"),
    (16, "달토끼", "moon_rabbit", "cute pixel art moon rabbit, 64x64 pixels, silver white fur, crescent moon motif, blue glow, subtle starlight, game asset, transparent background, tamagotchi style"),
    (17, "무지개뱀", "rainbow_snake", "cute pixel art rainbow snake, 64x64 pixels, colorful scales, blue accents, subtle shimmer effect, friendly face, game asset, transparent background, tamagotchi style"),
    (18, "구름고래", "cloud_whale", "cute pixel art cloud whale, 64x64 pixels, fluffy white body, blue sky theme, subtle floating effect, game asset, transparent background, tamagotchi style"),
    (19, "수정나비", "crystal_butterfly", "cute pixel art crystal butterfly, 64x64 pixels, gem wings, blue sparkles, subtle prismatic glow, game asset, transparent background, tamagotchi style"),
    (20, "숲요정", "forest_fairy", "cute pixel art forest fairy, 64x64 pixels, leaf wings, blue flower crown, subtle nature glow, tiny, game asset, transparent background, tamagotchi style"),
    (21, "별똥곰", "star_bear", "cute pixel art star bear, 64x64 pixels, constellation pattern fur, blue starlight, subtle cosmic aura, game asset, transparent background, tamagotchi style"),
    (22, "파도물개", "wave_seal", "cute pixel art wave seal, 64x64 pixels, ocean blue body, water splash, subtle bubble effect, game asset, transparent background, tamagotchi style"),
    (23, "안개늑대", "mist_wolf", "cute pixel art mist wolf, 64x64 pixels, gray fur, blue mist, subtle fog effect, mysterious, game asset, transparent background, tamagotchi style"),
    (24, "노을새", "sunset_bird", "cute pixel art sunset bird, 64x64 pixels, orange pink gradient feathers, blue accents, subtle warm glow, game asset, transparent background, tamagotchi style"),
    (25, "이끼거북", "moss_turtle", "cute pixel art moss turtle, 64x64 pixels, green shell with plants, blue flowers, subtle nature aura, game asset, transparent background, tamagotchi style"),

    # Common (26-50)
    (26, "슬라임", "slime", "cute pixel art slime, 64x64 pixels, simple round shape, bright pastel green, translucent, game asset, transparent background, tamagotchi style"),
    (27, "꼬마구름", "tiny_cloud", "cute pixel art tiny cloud, 64x64 pixels, simple fluffy shape, bright pastel white, happy face, game asset, transparent background, tamagotchi style"),
    (28, "잎새", "leaf", "cute pixel art leaf creature, 64x64 pixels, simple green leaf with face, bright pastel tones, game asset, transparent background, tamagotchi style"),
    (29, "물방울", "water_drop", "cute pixel art water droplet, 64x64 pixels, simple blue drop with face, bright pastel tones, shiny, game asset, transparent background, tamagotchi style"),
    (30, "돌멩이", "pebble", "cute pixel art rock creature, 64x64 pixels, simple gray stone with face, bright pastel tones, round, game asset, transparent background, tamagotchi style"),
    (31, "별똥별", "shooting_star", "cute pixel art shooting star, 64x64 pixels, simple yellow star with tail, bright pastel tones, game asset, transparent background, tamagotchi style"),
    (32, "꽃잎", "petal", "cute pixel art flower petal, 64x64 pixels, simple pink petal with face, bright pastel tones, game asset, transparent background, tamagotchi style"),
    (33, "솜뭉치", "cotton_ball", "cute pixel art cotton ball, 64x64 pixels, simple fluffy white puff with face, bright pastel tones, soft, game asset, transparent background, tamagotchi style"),
    (34, "젤리콩", "jelly_bean", "cute pixel art jelly bean, 64x64 pixels, simple candy shape with face, bright pastel colors, shiny, game asset, transparent background, tamagotchi style"),
    (35, "이끼돌", "moss_rock", "cute pixel art mossy rock, 64x64 pixels, simple stone with green moss and face, bright pastel tones, game asset, transparent background, tamagotchi style"),
    (36, "눈송이", "snowflake", "cute pixel art snowflake, 64x64 pixels, simple ice crystal with face, bright pastel blue white, game asset, transparent background, tamagotchi style"),
    (37, "반딧불", "firefly", "cute pixel art firefly, 64x64 pixels, simple glowing bug with face, bright pastel yellow, game asset, transparent background, tamagotchi style"),
    (38, "씨앗", "seed", "cute pixel art seed, 64x64 pixels, simple brown seed with face and tiny sprout, bright pastel tones, game asset, transparent background, tamagotchi style"),
    (39, "조약돌", "river_stone", "cute pixel art smooth pebble, 64x64 pixels, simple river stone with face, bright pastel gray, game asset, transparent background, tamagotchi style"),
    (40, "먼지토끼", "dust_bunny", "cute pixel art dust bunny, 64x64 pixels, simple gray fluff ball with ears and face, bright pastel tones, fuzzy, game asset, transparent background, tamagotchi style"),
    (41, "비누방울", "soap_bubble", "cute pixel art soap bubble, 64x64 pixels, simple rainbow bubble with face, bright pastel tones, shiny, game asset, transparent background, tamagotchi style"),
    (42, "도토리", "acorn", "cute pixel art acorn, 64x64 pixels, simple brown acorn with face, bright pastel tones, game asset, transparent background, tamagotchi style"),
    (43, "꿀방울", "honey_drop", "cute pixel art honey drop, 64x64 pixels, simple golden honey with face, bright pastel yellow, sticky, game asset, transparent background, tamagotchi style"),
    (44, "깃털", "feather", "cute pixel art feather, 64x64 pixels, simple soft feather with face, bright pastel white, fluffy, game asset, transparent background, tamagotchi style"),
    (45, "이슬", "dewdrop", "cute pixel art dewdrop, 64x64 pixels, simple morning dew with face, bright pastel blue, crystal clear, game asset, transparent background, tamagotchi style"),
    (46, "모래알", "sand_grain", "cute pixel art sand grain, 64x64 pixels, simple tiny sand with face, bright pastel beige, game asset, transparent background, tamagotchi style"),
    (47, "풀잎", "grass_blade", "cute pixel art grass blade, 64x64 pixels, simple green grass with face, bright pastel tones, game asset, transparent background, tamagotchi style"),
    (48, "나뭇가지", "twig", "cute pixel art twig, 64x64 pixels, simple brown stick with face, bright pastel tones, woody, game asset, transparent background, tamagotchi style"),
    (49, "진흙이", "mud_blob", "cute pixel art mud blob, 64x64 pixels, simple brown mud with face, bright pastel tones, squishy, game asset, transparent background, tamagotchi style"),
    (50, "버섯", "mushroom", "cute pixel art mushroom, 64x64 pixels, simple red mushroom with white dots and face, bright pastel tones, game asset, transparent background, tamagotchi style"),
]

# 알 데이터
EGGS = [
    ("fire_egg", "불꽃알", "cute pixel art fire egg, 64x64 pixels, orange with flame pattern, subtle glow, game asset, transparent background, tamagotchi style"),
    ("water_egg", "물방울알", "cute pixel art water egg, 64x64 pixels, blue with water drops, translucent, game asset, transparent background, tamagotchi style"),
    ("wind_egg", "바람알", "cute pixel art wind egg, 64x64 pixels, light green with swirl pattern, airy, game asset, transparent background, tamagotchi style"),
    ("earth_egg", "대지알", "cute pixel art earth egg, 64x64 pixels, brown with crystal pattern, solid, game asset, transparent background, tamagotchi style"),
    ("lightning_egg", "번개알", "cute pixel art lightning egg, 64x64 pixels, yellow with electric pattern, subtle sparks, game asset, transparent background, tamagotchi style"),
]


def generate_and_save(prompt: str, output_path: Path) -> bool:
    """이미지 생성 + 배경 제거 + 저장"""
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
        # 1. Imagen API로 이미지 생성
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

        # 2. 배경 제거
        transparent_img = remove(raw_img)

        # 3. 저장
        output_path.parent.mkdir(parents=True, exist_ok=True)
        with open(output_path, "wb") as f:
            f.write(transparent_img)

        return True

    except Exception as e:
        print(f"  에러: {e}")
        return False


def generate_single(index: int):
    """단일 크리처 생성 (1-50) 또는 알 (51-55)"""
    if 1 <= index <= 50:
        num, name_kr, name_en, prompt = CREATURES[index - 1]
        output_path = CREATURES_DIR / f"{num}.png"
        print(f"[{num}/50] {name_kr} ({name_en}) 생성 중...")
    elif 51 <= index <= 55:
        egg_idx = index - 51
        name_en, name_kr, prompt = EGGS[egg_idx]
        output_path = EGGS_DIR / f"{name_en}.png"
        print(f"[알 {egg_idx+1}/5] {name_kr} ({name_en}) 생성 중...")
    else:
        print("잘못된 인덱스 (1-55)")
        return False

    if generate_and_save(prompt, output_path):
        print(f"  ✅ 저장됨: {output_path}")
        return True
    else:
        print(f"  ❌ 실패")
        return False


if __name__ == "__main__":
    if len(sys.argv) > 1:
        idx = int(sys.argv[1])
        generate_single(idx)
    else:
        print("사용법: python generate_assets_gemini.py [번호]")
        print("  1-50: 크리처")
        print("  51-55: 알")
