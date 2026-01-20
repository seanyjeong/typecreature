#!/usr/bin/env python3
"""
워터마크 및 남은 체크무늬 제거
하단 영역의 텍스트 워터마크 + 추가 정리
"""

from PIL import Image
from pathlib import Path

def remove_bottom_watermark(img, bottom_height=80):
    """이미지 하단 영역에서 회색 텍스트 워터마크 제거"""
    img = img.convert('RGBA')
    width, height = img.size
    pixels = img.load()

    removed = 0

    # 하단 영역만 처리
    for y in range(height - bottom_height, height):
        for x in range(width):
            pixel = pixels[x, y]
            r, g, b, a = pixel

            if a == 0:  # 이미 투명
                continue

            # 회색 계열 텍스트 (워터마크는 보통 회색)
            # "644 6464" 같은 텍스트는 회색 계열
            is_gray = (abs(r - g) < 20 and abs(g - b) < 20 and abs(r - b) < 20)
            is_dark_gray = 50 <= r <= 150 and 50 <= g <= 150 and 50 <= b <= 150
            is_light_gray = 150 <= r <= 220 and 150 <= g <= 220 and 150 <= b <= 220

            # 주변에 투명 픽셀이 많으면 워터마크일 가능성 높음
            if is_gray and (is_dark_gray or is_light_gray):
                # 주변 투명 픽셀 체크
                transparent_count = 0
                total = 0
                for dy in range(-3, 4):
                    for dx in range(-3, 4):
                        nx, ny = x + dx, y + dy
                        if 0 <= nx < width and 0 <= ny < height:
                            total += 1
                            if pixels[nx, ny][3] == 0:
                                transparent_count += 1

                # 주변의 30% 이상이 투명이면 워터마크로 간주
                if total > 0 and transparent_count / total > 0.3:
                    pixels[x, y] = (0, 0, 0, 0)
                    removed += 1

    return img, removed

def aggressive_checkered_removal(img):
    """더 공격적인 체크무늬 제거 (크리처 주변부)"""
    img = img.convert('RGBA')
    width, height = img.size
    pixels = img.load()

    removed = 0

    for y in range(height):
        for x in range(width):
            pixel = pixels[x, y]
            r, g, b, a = pixel

            if a == 0:
                continue

            # 체크무늬 패턴 색상
            is_checkered_gray = (abs(r - g) < 10 and abs(g - b) < 10 and
                                 190 <= r <= 220 and 190 <= g <= 220 and 190 <= b <= 220)
            is_checkered_white = r >= 245 and g >= 245 and b >= 245

            if is_checkered_gray or is_checkered_white:
                # 주변 투명 픽셀 비율 확인
                transparent_count = 0
                total = 0
                window = 5

                for dy in range(-window, window + 1):
                    for dx in range(-window, window + 1):
                        nx, ny = x + dx, y + dy
                        if 0 <= nx < width and 0 <= ny < height:
                            total += 1
                            if pixels[nx, ny][3] == 0:
                                transparent_count += 1

                # 주변의 25% 이상이 투명이면 제거
                if total > 0 and transparent_count / total > 0.25:
                    pixels[x, y] = (0, 0, 0, 0)
                    removed += 1

    return img, removed

def process_image(img_path):
    """이미지 처리"""
    try:
        img = Image.open(img_path)
        img = img.convert('RGBA')

        print(f"  {img_path.name}:")

        # 1. 워터마크 제거
        img, wm_removed = remove_bottom_watermark(img)
        print(f"    워터마크: {wm_removed}픽셀")

        # 2. 추가 체크무늬 제거 (2회)
        total_ck = 0
        for i in range(2):
            img, ck_removed = aggressive_checkered_removal(img)
            total_ck += ck_removed
            if ck_removed == 0:
                break
        print(f"    체크무늬: {total_ck}픽셀")

        img.save(img_path, 'PNG')
        return True

    except Exception as e:
        print(f"    에러: {e}")
        return False

def main():
    base_dir = Path(__file__).parent.parent / "TypingTamagotchi" / "Assets"
    creatures_dir = base_dir / "Creatures"
    eggs_dir = base_dir / "Eggs"

    print("=" * 50)
    print("워터마크 및 잔여 체크무늬 제거")
    print("=" * 50)

    count = 0

    print("\n크리처:")
    for png in sorted(creatures_dir.glob("*.png")):
        if process_image(png):
            count += 1

    print("\n알:")
    for png in sorted(eggs_dir.glob("*.png")):
        if process_image(png):
            count += 1

    print(f"\n완료! {count}개 파일 처리")

if __name__ == "__main__":
    main()
