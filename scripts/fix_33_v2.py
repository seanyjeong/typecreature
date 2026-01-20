#!/usr/bin/env python3
"""
솜뭉치(33) v2 - 체크무늬 패턴만 제거
"""

from PIL import Image
from pathlib import Path
from collections import deque

def is_checker_gray(r, g, b):
    """체크무늬의 회색인지 (215-225 균일 회색)"""
    return (210 <= r <= 230 and 210 <= g <= 230 and 210 <= b <= 230 and
            abs(r - g) < 5 and abs(g - b) < 5)

def is_checker_white(r, g, b):
    """체크무늬의 흰색인지 (250-255 균일)"""
    return r >= 250 and g >= 250 and b >= 250

def is_checkered_pattern(pixels, x, y, width, height):
    """이 픽셀이 체크무늬 패턴인지 확인"""
    p = pixels[x, y]
    if p[3] == 0:
        return False

    r, g, b = p[0], p[1], p[2]

    curr_gray = is_checker_gray(r, g, b)
    curr_white = is_checker_white(r, g, b)

    if not (curr_gray or curr_white):
        return False

    # 대각선 방향 이웃이 반대 색인지 확인 (체크무늬 특성)
    diag_opposite = 0
    for dx, dy in [(-1,-1), (-1,1), (1,-1), (1,1)]:
        nx, ny = x + dx, y + dy
        if 0 <= nx < width and 0 <= ny < height:
            np = pixels[nx, ny]
            if np[3] > 0:
                nr, ng, nb = np[0], np[1], np[2]
                if curr_gray and is_checker_white(nr, ng, nb):
                    diag_opposite += 1
                elif curr_white and is_checker_gray(nr, ng, nb):
                    diag_opposite += 1

    return diag_opposite >= 2

def fix_creature():
    base = Path(__file__).parent.parent / "TypingTamagotchi" / "Assets" / "Creatures"
    img = Image.open(base / "33.png").convert('RGBA')
    width, height = img.size
    pixels = img.load()

    # 가장자리에서 체크무늬만 제거 (BFS)
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

            # 투명이면 계속
            if p[3] == 0:
                for dx, dy in [(-1,0), (1,0), (0,-1), (0,1)]:
                    queue.append((x + dx, y + dy))
                continue

            # 체크무늬면 제거하고 계속
            if is_checkered_pattern(pixels, x, y, width, height):
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

    print(f"체크무늬 제거: {len(to_remove)}개")

    for x, y in to_remove:
        pixels[x, y] = (0, 0, 0, 0)

    # 워터마크 제거
    visited2 = [[False] * height for _ in range(width)]

    def find_comp(sx, sy):
        queue = deque([(sx, sy)])
        comp = []
        while queue:
            x, y = queue.popleft()
            if x < 0 or x >= width or y < 0 or y >= height:
                continue
            if visited2[x][y]:
                continue
            if pixels[x, y][3] == 0:
                continue
            visited2[x][y] = True
            comp.append((x, y))
            for dx, dy in [(-1,0), (1,0), (0,-1), (0,1)]:
                queue.append((x + dx, y + dy))
        return comp

    wm = 0
    for y in range(height - 120, height):
        for x in range(width):
            if not visited2[x][y] and pixels[x, y][3] > 0:
                comp = find_comp(x, y)
                if comp:
                    min_y = min(p[1] for p in comp)
                    if min_y > height - 150:
                        for px, py in comp:
                            pixels[px, py] = (0, 0, 0, 0)
                            wm += 1

    print(f"워터마크 제거: {wm}개")

    img.save(base / "33.png", 'PNG')
    print("저장!")

if __name__ == "__main__":
    fix_creature()
