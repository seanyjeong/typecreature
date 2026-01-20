#!/usr/bin/env python3
"""
솜뭉치 전용 수정 - 외곽선 안쪽 모든 픽셀 보존
"""

from PIL import Image
from pathlib import Path
from collections import deque

def process_cotton_ball(img_path):
    img = Image.open(img_path).convert('RGBA')
    width, height = img.size
    pixels = img.load()

    # 1. 외곽선(검은색) 픽셀 찾기
    outline = set()
    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            if a > 0 and r < 80 and g < 80 and b < 80:
                outline.add((x, y))

    print(f"외곽선 픽셀: {len(outline)}개")

    # 2. 가장자리에서 BFS로 외부 영역 찾기 (외곽선에서 멈춤)
    outside = set()
    visited = [[False] * height for _ in range(width)]

    def bfs_outside(start_x, start_y):
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

    # 네 가장자리에서 시작
    for x in range(width):
        bfs_outside(x, 0)
        bfs_outside(x, height - 1)
    for y in range(height):
        bfs_outside(0, y)
        bfs_outside(width - 1, y)

    print(f"외부 영역: {len(outside)}개")

    # 3. 외부 영역만 투명으로 (내부는 보존)
    removed = 0
    for x, y in outside:
        if pixels[x, y][3] > 0:  # 불투명한 픽셀만
            pixels[x, y] = (0, 0, 0, 0)
            removed += 1

    print(f"제거된 픽셀: {removed}개")

    # 4. 하단 워터마크 제거 (분리된 컴포넌트)
    # 하단 영역에서 메인과 연결되지 않은 것 제거
    visited2 = [[False] * height for _ in range(width)]

    def find_component(start_x, start_y):
        queue = deque([(start_x, start_y)])
        component = []

        while queue:
            x, y = queue.popleft()
            if x < 0 or x >= width or y < 0 or y >= height:
                continue
            if visited2[x][y]:
                continue
            if pixels[x, y][3] == 0:
                continue

            visited2[x][y] = True
            component.append((x, y))

            for dx, dy in [(-1,0), (1,0), (0,-1), (0,1)]:
                queue.append((x + dx, y + dy))

        return component

    # 하단 80픽셀 스캔
    watermark_removed = 0
    for y in range(height - 100, height):
        for x in range(width):
            if not visited2[x][y] and pixels[x, y][3] > 0:
                comp = find_component(x, y)
                if comp:
                    min_y = min(p[1] for p in comp)
                    # 상단으로 확장되지 않으면 워터마크
                    if min_y > height - 150:
                        for px, py in comp:
                            pixels[px, py] = (0, 0, 0, 0)
                            watermark_removed += 1

    print(f"워터마크 제거: {watermark_removed}개")

    img.save(img_path, 'PNG')
    print("저장 완료!")

if __name__ == "__main__":
    base = Path(__file__).parent.parent / "TypingTamagotchi" / "Assets" / "Creatures"
    process_cotton_ball(base / "33.png")
