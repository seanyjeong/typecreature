#!/usr/bin/env python3
"""
체크무늬 배경 제거 스크립트 v2
더 정확한 체크무늬 패턴 감지 및 제거
"""

from PIL import Image
import os
from pathlib import Path
import numpy as np

def is_checkered_color(r, g, b, a):
    """체크무늬 색상인지 확인 (흰색 또는 회색)"""
    if a < 255:  # 이미 투명한 경우
        return False

    # 회색 계열 (체크무늬의 어두운 부분) - RGB가 비슷하고 180-230 범위
    is_gray = (abs(r - g) < 10 and abs(g - b) < 10 and abs(r - b) < 10 and
               170 <= r <= 235 and 170 <= g <= 235 and 170 <= b <= 235)

    # 흰색 계열 (체크무늬의 밝은 부분)
    is_white = r >= 240 and g >= 240 and b >= 240

    # 밝은 회색 (거의 흰색에 가까운)
    is_light_gray = (abs(r - g) < 5 and abs(g - b) < 5 and r >= 235)

    return is_gray or is_white or is_light_gray

def detect_checkered_pattern(img, sample_size=20):
    """이미지 모서리에서 체크무늬 패턴 감지"""
    width, height = img.size
    pixels = img.load()

    # 네 모서리에서 샘플링
    corners = [
        (0, 0),  # 좌상
        (width - sample_size, 0),  # 우상
        (0, height - sample_size),  # 좌하
        (width - sample_size, height - sample_size)  # 우하
    ]

    checkered_count = 0
    total_samples = 0

    for cx, cy in corners:
        for y in range(cy, min(cy + sample_size, height)):
            for x in range(cx, min(cx + sample_size, width)):
                pixel = pixels[x, y]
                if len(pixel) == 4:
                    r, g, b, a = pixel
                else:
                    r, g, b = pixel
                    a = 255

                if is_checkered_color(r, g, b, a):
                    checkered_count += 1
                total_samples += 1

    return checkered_count / total_samples > 0.5 if total_samples > 0 else False

def flood_fill_remove(img, tolerance=30):
    """플러드 필 방식으로 가장자리에서 체크무늬/배경 제거"""
    img = img.convert('RGBA')
    width, height = img.size
    pixels = img.load()

    # 방문 여부 체크
    visited = [[False] * height for _ in range(width)]

    # 제거할 픽셀들
    to_remove = set()

    def should_remove(r, g, b, a):
        if a < 255:
            return False
        return is_checkered_color(r, g, b, a)

    def bfs_fill(start_x, start_y):
        """BFS로 연결된 배경 픽셀 찾기"""
        queue = [(start_x, start_y)]

        while queue:
            x, y = queue.pop(0)

            if x < 0 or x >= width or y < 0 or y >= height:
                continue
            if visited[x][y]:
                continue

            visited[x][y] = True
            pixel = pixels[x, y]

            if len(pixel) == 4:
                r, g, b, a = pixel
            else:
                r, g, b = pixel
                a = 255

            if should_remove(r, g, b, a):
                to_remove.add((x, y))
                # 8방향으로 확장
                for dx in [-1, 0, 1]:
                    for dy in [-1, 0, 1]:
                        if dx != 0 or dy != 0:
                            queue.append((x + dx, y + dy))

    # 가장자리에서 시작
    # 상단과 하단
    for x in range(width):
        bfs_fill(x, 0)
        bfs_fill(x, height - 1)

    # 좌측과 우측
    for y in range(height):
        bfs_fill(0, y)
        bfs_fill(width - 1, y)

    # 제거할 픽셀들을 투명으로
    for x, y in to_remove:
        pixels[x, y] = (0, 0, 0, 0)

    return img, len(to_remove)

def remove_isolated_checkered(img, window_size=5):
    """크리처 내부에 남은 고립된 체크무늬 제거"""
    img = img.convert('RGBA')
    width, height = img.size
    pixels = img.load()

    removed = 0

    for y in range(height):
        for x in range(width):
            pixel = pixels[x, y]
            if len(pixel) == 4:
                r, g, b, a = pixel
            else:
                r, g, b = pixel
                a = 255

            if a == 0:  # 이미 투명
                continue

            if is_checkered_color(r, g, b, a):
                # 주변 투명 픽셀 수 확인
                transparent_count = 0
                total_neighbors = 0

                for dy in range(-window_size, window_size + 1):
                    for dx in range(-window_size, window_size + 1):
                        nx, ny = x + dx, y + dy
                        if 0 <= nx < width and 0 <= ny < height and (dx != 0 or dy != 0):
                            total_neighbors += 1
                            np = pixels[nx, ny]
                            if len(np) == 4 and np[3] == 0:
                                transparent_count += 1

                # 주변의 40% 이상이 투명이면 이 픽셀도 제거
                if total_neighbors > 0 and transparent_count / total_neighbors > 0.4:
                    pixels[x, y] = (0, 0, 0, 0)
                    removed += 1

    return img, removed

def process_image(input_path, output_path):
    """이미지 처리"""
    try:
        img = Image.open(input_path)
        original_mode = img.mode
        img = img.convert('RGBA')

        print(f"  처리 중: {input_path.name}")

        # 1단계: 플러드 필로 가장자리 배경 제거
        img, removed1 = flood_fill_remove(img)
        print(f"    1단계 (가장자리): {removed1}픽셀 제거")

        # 2단계: 고립된 체크무늬 제거 (여러 번 반복)
        total_removed2 = 0
        for i in range(3):  # 3번 반복
            img, removed2 = remove_isolated_checkered(img, window_size=3)
            total_removed2 += removed2
            if removed2 == 0:
                break
        print(f"    2단계 (고립): {total_removed2}픽셀 제거")

        # 저장
        img.save(output_path, 'PNG')
        return True, removed1 + total_removed2

    except Exception as e:
        print(f"    에러: {e}")
        return False, 0

def main():
    base_dir = Path(__file__).parent.parent / "TypingTamagotchi" / "Assets"

    # Creatures 폴더
    creatures_dir = base_dir / "Creatures"
    eggs_dir = base_dir / "Eggs"

    print("=" * 50)
    print("체크무늬 배경 제거 v2")
    print("=" * 50)

    success_count = 0
    total_removed = 0

    # 크리처 처리
    print("\n크리처 처리 중...")
    for png_file in sorted(creatures_dir.glob("*.png")):
        success, removed = process_image(png_file, png_file)
        if success:
            success_count += 1
            total_removed += removed

    # 알 처리
    print("\n알 처리 중...")
    for png_file in sorted(eggs_dir.glob("*.png")):
        success, removed = process_image(png_file, png_file)
        if success:
            success_count += 1
            total_removed += removed

    print("\n" + "=" * 50)
    print(f"완료! 처리된 파일: {success_count}, 총 제거된 픽셀: {total_removed}")
    print("=" * 50)

if __name__ == "__main__":
    main()
