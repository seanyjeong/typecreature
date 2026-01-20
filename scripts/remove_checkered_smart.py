#!/usr/bin/env python3
"""
체크무늬 패턴만 정확히 감지해서 제거
솜뭉치 같은 흰색 크리처용
"""

from PIL import Image
from pathlib import Path
from collections import deque

def is_checkered_pixel(pixels, x, y, width, height):
    """이 픽셀이 체크무늬 패턴의 일부인지 확인"""
    p = pixels[x, y]
    if p[3] == 0:
        return False

    r, g, b = p[0], p[1], p[2]

    # 체크무늬는 흰색(~255) 또는 회색(~204) 두 가지
    is_checker_white = r >= 250 and g >= 250 and b >= 250
    is_checker_gray = 195 <= r <= 215 and 195 <= g <= 215 and 195 <= b <= 215

    if not (is_checker_white or is_checker_gray):
        return False

    # 주변 픽셀이 반대 색인지 확인 (체크무늬 패턴 특성)
    opposite_count = 0
    for dx, dy in [(-1,0), (1,0), (0,-1), (0,1)]:
        nx, ny = x + dx, y + dy
        if 0 <= nx < width and 0 <= ny < height:
            np = pixels[nx, ny]
            if np[3] > 0:
                nr, ng, nb = np[0], np[1], np[2]
                np_white = nr >= 250 and ng >= 250 and nb >= 250
                np_gray = 195 <= nr <= 215 and 195 <= ng <= 215 and 195 <= nb <= 215

                # 현재가 흰색이면 이웃이 회색이어야 함 (또는 반대)
                if is_checker_white and np_gray:
                    opposite_count += 1
                elif is_checker_gray and np_white:
                    opposite_count += 1

    # 2개 이상의 이웃이 반대 색이면 체크무늬
    return opposite_count >= 2

def remove_checkered_from_edges(img):
    """가장자리에서 시작해서 체크무늬만 제거"""
    img = img.convert('RGBA')
    width, height = img.size
    pixels = img.load()

    visited = [[False] * height for _ in range(width)]
    to_remove = set()

    def bfs(start_x, start_y):
        queue = deque([(start_x, start_y)])

        while queue:
            x, y = queue.popleft()

            if x < 0 or x >= width or y < 0 or y >= height:
                continue
            if visited[x][y]:
                continue

            visited[x][y] = True

            p = pixels[x, y]

            # 투명이면 계속 탐색
            if p[3] == 0:
                for dx, dy in [(-1,0), (1,0), (0,-1), (0,1)]:
                    queue.append((x + dx, y + dy))
                continue

            # 체크무늬면 제거하고 계속 탐색
            if is_checkered_pixel(pixels, x, y, width, height):
                to_remove.add((x, y))
                for dx, dy in [(-1,0), (1,0), (0,-1), (0,1)]:
                    queue.append((x + dx, y + dy))

    # 가장자리에서 시작
    for x in range(width):
        bfs(x, 0)
        bfs(x, height - 1)
    for y in range(height):
        bfs(0, y)
        bfs(width - 1, y)

    for x, y in to_remove:
        pixels[x, y] = (0, 0, 0, 0)

    return img, len(to_remove)

def remove_watermark_text(img):
    """하단 워터마크 텍스트 제거"""
    width, height = img.size
    pixels = img.load()

    visited = [[False] * height for _ in range(width)]

    def find_component(start_x, start_y):
        queue = deque([(start_x, start_y)])
        component = []

        while queue:
            x, y = queue.popleft()
            if x < 0 or x >= width or y < 0 or y >= height:
                continue
            if visited[x][y]:
                continue
            if pixels[x, y][3] == 0:
                continue

            visited[x][y] = True
            component.append((x, y))

            for dx, dy in [(-1,0), (1,0), (0,-1), (0,1)]:
                queue.append((x + dx, y + dy))

        return component

    removed = 0
    for y in range(height - 100, height):
        for x in range(width):
            if not visited[x][y] and pixels[x, y][3] > 0:
                comp = find_component(x, y)
                if comp:
                    min_y = min(p[1] for p in comp)
                    max_y = max(p[1] for p in comp)
                    # 하단에만 있고 위로 확장 안되면 워터마크
                    if min_y > height - 120 and max_y - min_y < 80:
                        for px, py in comp:
                            pixels[px, py] = (0, 0, 0, 0)
                            removed += 1

    return img, removed

def main():
    import sys

    if len(sys.argv) < 2:
        print("사용법: python remove_checkered_smart.py <이미지번호>")
        return

    base_dir = Path(__file__).parent.parent / "TypingTamagotchi" / "Assets" / "Creatures"

    for num in sys.argv[1:]:
        img_path = base_dir / f"{num}.png"
        if img_path.exists():
            print(f"처리: {num}.png")
            img = Image.open(img_path)

            img, ck = remove_checkered_from_edges(img)
            print(f"  체크무늬: {ck}픽셀")

            img, wm = remove_watermark_text(img)
            print(f"  워터마크: {wm}픽셀")

            img.save(img_path, 'PNG')
            print("  저장 완료")

if __name__ == "__main__":
    main()
