#!/usr/bin/env python3
"""
솜뭉치(33) 최종 수정
- 외곽선 바깥의 회색 체크무늬만 제거
- 내부 흰색은 보존
"""

from PIL import Image
from pathlib import Path
from collections import deque

def fix_creature_33():
    base = Path(__file__).parent.parent / "TypingTamagotchi" / "Assets" / "Creatures"
    img = Image.open(base / "33.png").convert('RGBA')
    width, height = img.size
    pixels = img.load()

    # 1. 외곽선(어두운 색) 픽셀 찾기
    outline = set()
    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            if a > 0 and r < 80 and g < 80 and b < 100:
                outline.add((x, y))

    print(f"외곽선 픽셀: {len(outline)}개")

    # 2. 가장자리에서 BFS로 외부 영역 찾기 (외곽선에서 멈춤)
    outside = set()
    visited = [[False] * height for _ in range(width)]

    def bfs(start_x, start_y):
        queue = deque([(start_x, start_y)])

        while queue:
            x, y = queue.popleft()

            if x < 0 or x >= width or y < 0 or y >= height:
                continue
            if visited[x][y]:
                continue
            if (x, y) in outline:  # 외곽선에서 멈춤
                continue

            visited[x][y] = True
            outside.add((x, y))

            for dx, dy in [(-1,0), (1,0), (0,-1), (0,1)]:
                queue.append((x + dx, y + dy))

    # 가장자리에서 시작
    for x in range(width):
        bfs(x, 0)
        bfs(x, height - 1)
    for y in range(height):
        bfs(0, y)
        bfs(width - 1, y)

    print(f"외부 영역: {len(outside)}개")

    # 3. 외부 영역 투명으로 (체크무늬 제거)
    removed = 0
    for x, y in outside:
        if pixels[x, y][3] > 0:
            pixels[x, y] = (0, 0, 0, 0)
            removed += 1

    print(f"체크무늬 제거: {removed}개")

    # 4. 외곽선 안쪽에 있지만 체크무늬인 픽셀도 제거 (구멍 채우기)
    # 외곽선 안쪽의 회색(215-225) 픽셀 중 주변이 투명인 것 제거
    for _ in range(3):  # 반복
        extra_removed = 0
        for y in range(height):
            for x in range(width):
                if (x, y) in outside or (x, y) in outline:
                    continue
                r, g, b, a = pixels[x, y]
                if a == 0:
                    continue

                # 체크무늬 회색인지
                is_gray = 210 <= r <= 230 and 210 <= g <= 230 and 210 <= b <= 230

                if is_gray:
                    # 주변 투명 픽셀 수
                    trans_count = 0
                    for dx, dy in [(-1,0), (1,0), (0,-1), (0,1)]:
                        nx, ny = x + dx, y + dy
                        if 0 <= nx < width and 0 <= ny < height:
                            if pixels[nx, ny][3] == 0:
                                trans_count += 1

                    if trans_count >= 2:
                        pixels[x, y] = (0, 0, 0, 0)
                        extra_removed += 1

        print(f"추가 제거: {extra_removed}개")
        if extra_removed == 0:
            break

    # 5. 워터마크 제거 (하단 분리 컴포넌트)
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

    wm_removed = 0
    for y in range(height - 120, height):
        for x in range(width):
            if not visited2[x][y] and pixels[x, y][3] > 0:
                comp = find_comp(x, y)
                if comp:
                    min_y = min(p[1] for p in comp)
                    if min_y > height - 150:  # 하단에만 있으면 워터마크
                        for px, py in comp:
                            pixels[px, py] = (0, 0, 0, 0)
                            wm_removed += 1

    print(f"워터마크 제거: {wm_removed}개")

    img.save(base / "33.png", 'PNG')
    print("저장 완료!")

if __name__ == "__main__":
    fix_creature_33()
