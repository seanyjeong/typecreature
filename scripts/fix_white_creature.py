#!/usr/bin/env python3
"""
흰색 크리처 전용 배경 제거
크리처 내부 흰색은 보존하면서 외곽 체크무늬만 제거
"""

from PIL import Image
from pathlib import Path
from collections import deque

def has_outline(pixels, x, y, width, height, check_radius=3):
    """이 픽셀 주변에 검은 외곽선이 있는지 확인"""
    for dy in range(-check_radius, check_radius + 1):
        for dx in range(-check_radius, check_radius + 1):
            nx, ny = x + dx, y + dy
            if 0 <= nx < width and 0 <= ny < height:
                p = pixels[nx, ny]
                if len(p) >= 3:
                    r, g, b = p[0], p[1], p[2]
                    # 검은색 또는 매우 어두운 색 (외곽선)
                    if r < 50 and g < 50 and b < 50:
                        return True
    return False

def remove_outer_checkered(img):
    """외곽 체크무늬만 제거 (내부 흰색 보존)"""
    img = img.convert('RGBA')
    width, height = img.size
    pixels = img.load()

    # 1단계: 외곽선 안쪽 영역 마킹 (보호 영역)
    protected = [[False] * height for _ in range(width)]

    # 외곽선(검은색) 픽셀 찾기
    outline_pixels = set()
    for y in range(height):
        for x in range(width):
            p = pixels[x, y]
            r, g, b = p[0], p[1], p[2]
            if r < 60 and g < 60 and b < 60 and p[3] > 200:
                outline_pixels.add((x, y))

    # 외곽선 안쪽을 BFS로 채우기 (보호 영역)
    # 중심에서 시작
    center_x, center_y = width // 2, height // 2

    # 중심이 투명이면 불투명한 픽셀 찾기
    if pixels[center_x, center_y][3] == 0:
        for y in range(height):
            for x in range(width):
                if pixels[x, y][3] > 0:
                    center_x, center_y = x, y
                    break

    # BFS로 내부 영역 마킹
    queue = deque([(center_x, center_y)])
    visited = [[False] * height for _ in range(width)]

    while queue:
        x, y = queue.popleft()

        if x < 0 or x >= width or y < 0 or y >= height:
            continue
        if visited[x][y]:
            continue
        if (x, y) in outline_pixels:
            protected[x][y] = True
            continue

        visited[x][y] = True
        protected[x][y] = True

        for dx, dy in [(-1,0), (1,0), (0,-1), (0,1)]:
            queue.append((x + dx, y + dy))

    # 2단계: 보호되지 않은 영역의 체크무늬/흰색/회색 제거
    removed = 0
    for y in range(height):
        for x in range(width):
            if protected[x][y]:
                continue

            p = pixels[x, y]
            if p[3] == 0:
                continue

            r, g, b = p[0], p[1], p[2]

            # 체크무늬 색상 (흰색 또는 회색)
            is_checkered = (abs(r - g) < 15 and abs(g - b) < 15 and
                          ((r >= 200) or (170 <= r <= 220)))

            if is_checkered:
                pixels[x, y] = (0, 0, 0, 0)
                removed += 1

    # 3단계: 하단 워터마크 제거 (보호 영역 밖)
    for y in range(height - 60, height):
        for x in range(width):
            if protected[x][y]:
                continue
            p = pixels[x, y]
            if p[3] > 0:
                pixels[x, y] = (0, 0, 0, 0)
                removed += 1

    return img, removed

def process_image(img_path):
    """이미지 처리"""
    try:
        img = Image.open(img_path)
        img, removed = remove_outer_checkered(img)

        print(f"  {img_path.name}: {removed}픽셀 제거")
        img.save(img_path, 'PNG')
        return True

    except Exception as e:
        print(f"  {img_path.name}: 에러 - {e}")
        return False

def main():
    import sys

    if len(sys.argv) < 2:
        print("사용법: python fix_white_creature.py <이미지번호>")
        print("예: python fix_white_creature.py 33")
        return

    base_dir = Path(__file__).parent.parent / "TypingTamagotchi" / "Assets" / "Creatures"

    for num in sys.argv[1:]:
        img_path = base_dir / f"{num}.png"
        if img_path.exists():
            print(f"처리 중: {img_path}")
            process_image(img_path)
        else:
            print(f"파일 없음: {img_path}")

if __name__ == "__main__":
    main()
