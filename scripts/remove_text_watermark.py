#!/usr/bin/env python3
"""
텍스트 워터마크 강제 제거
하단 우측 영역의 모든 비-투명 픽셀 제거
"""

from PIL import Image
from pathlib import Path

def remove_bottom_right_text(img, bottom_height=60, right_width=200):
    """하단 우측 영역 정리"""
    img = img.convert('RGBA')
    width, height = img.size
    pixels = img.load()

    removed = 0

    # 하단 우측 영역
    start_x = width - right_width
    start_y = height - bottom_height

    for y in range(start_y, height):
        for x in range(start_x, width):
            pixel = pixels[x, y]
            r, g, b, a = pixel

            if a == 0:
                continue

            # 회색~검은색 텍스트 (워터마크)
            is_text = (abs(r - g) < 30 and abs(g - b) < 30 and
                      ((50 <= r <= 180) or r < 50))  # 어두운 회색 또는 검은색

            # 또는 주변에 투명 픽셀이 많으면 텍스트일 가능성
            if is_text:
                transparent_count = 0
                total = 0
                for dy in range(-2, 3):
                    for dx in range(-2, 3):
                        nx, ny = x + dx, y + dy
                        if 0 <= nx < width and 0 <= ny < height:
                            total += 1
                            if pixels[nx, ny][3] == 0:
                                transparent_count += 1

                if total > 0 and transparent_count / total > 0.2:
                    pixels[x, y] = (0, 0, 0, 0)
                    removed += 1

    return img, removed

def scan_and_remove_text_bottom(img, scan_height=100):
    """하단 전체를 스캔해서 텍스트 제거"""
    img = img.convert('RGBA')
    width, height = img.size
    pixels = img.load()

    removed = 0

    for y in range(height - scan_height, height):
        for x in range(width):
            pixel = pixels[x, y]
            r, g, b, a = pixel

            if a == 0:
                continue

            # 숫자/텍스트는 보통 단색의 어두운 색상
            # 50-150 범위의 균일한 회색
            is_text_color = (abs(r - g) < 15 and abs(g - b) < 15 and
                            40 <= r <= 160 and 40 <= g <= 160 and 40 <= b <= 160)

            if is_text_color:
                # 이 픽셀이 크리처의 일부인지 텍스트인지 구분
                # 주변 픽셀 확인
                colorful_neighbors = 0
                transparent_neighbors = 0
                total = 0

                for dy in range(-4, 5):
                    for dx in range(-4, 5):
                        nx, ny = x + dx, y + dy
                        if 0 <= nx < width and 0 <= ny < height and (dx != 0 or dy != 0):
                            np = pixels[nx, ny]
                            total += 1
                            if np[3] == 0:
                                transparent_neighbors += 1
                            elif abs(np[0] - np[1]) > 20 or abs(np[1] - np[2]) > 20:
                                # 다채로운 색상 = 크리처의 일부일 가능성
                                colorful_neighbors += 1

                # 주변이 대부분 투명이고 다채로운 색이 없으면 텍스트
                if total > 0:
                    trans_ratio = transparent_neighbors / total
                    colorful_ratio = colorful_neighbors / total

                    if trans_ratio > 0.4 and colorful_ratio < 0.1:
                        pixels[x, y] = (0, 0, 0, 0)
                        removed += 1

    return img, removed

def process_image(img_path):
    """이미지 처리"""
    try:
        img = Image.open(img_path)
        img = img.convert('RGBA')

        # 1. 하단 우측 텍스트 제거
        img, r1 = remove_bottom_right_text(img, bottom_height=80, right_width=250)

        # 2. 하단 전체 스캔
        img, r2 = scan_and_remove_text_bottom(img, scan_height=100)

        total = r1 + r2
        if total > 0:
            print(f"  {img_path.name}: {total}픽셀 제거")
            img.save(img_path, 'PNG')

        return total

    except Exception as e:
        print(f"  {img_path.name}: 에러 - {e}")
        return 0

def main():
    base_dir = Path(__file__).parent.parent / "TypingTamagotchi" / "Assets"

    print("=" * 50)
    print("텍스트 워터마크 제거")
    print("=" * 50)

    total_removed = 0

    for folder in ["Creatures", "Eggs"]:
        folder_path = base_dir / folder
        print(f"\n{folder}:")
        for png in sorted(folder_path.glob("*.png")):
            removed = process_image(png)
            total_removed += removed

    print(f"\n총 {total_removed}픽셀 제거됨")

if __name__ == "__main__":
    main()
