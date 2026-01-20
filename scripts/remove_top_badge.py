#!/usr/bin/env python3
"""
상단 좌측 배지/워터마크 제거
"""

from PIL import Image
from pathlib import Path
from collections import deque

def remove_top_left_region(img, region_width=200, region_height=120):
    """상단 좌측 영역의 분리된 요소 제거"""
    img = img.convert('RGBA')
    width, height = img.size
    pixels = img.load()

    # 상단 좌측 영역에서 컴포넌트 찾기
    visited = [[False] * height for _ in range(width)]

    def bfs(start_x, start_y):
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

    # 상단 좌측 영역 스캔
    for y in range(min(region_height, height)):
        for x in range(min(region_width, width)):
            if not visited[x][y] and pixels[x, y][3] > 0:
                component = bfs(x, y)
                if component:
                    # 이 컴포넌트가 상단 좌측에만 있는지 확인
                    max_x = max(p[0] for p in component)
                    max_y = max(p[1] for p in component)

                    # 컴포넌트가 이미지 중심으로 확장되지 않으면 배지로 간주
                    if max_x < width * 0.5 and max_y < height * 0.4:
                        for px, py in component:
                            pixels[px, py] = (0, 0, 0, 0)
                            removed += 1

    return img, removed

def main():
    import sys

    if len(sys.argv) < 2:
        print("사용법: python remove_top_badge.py <이미지번호>")
        return

    base_dir = Path(__file__).parent.parent / "TypingTamagotchi" / "Assets" / "Creatures"

    for num in sys.argv[1:]:
        img_path = base_dir / f"{num}.png"
        if img_path.exists():
            img = Image.open(img_path)
            img, removed = remove_top_left_region(img)
            print(f"{num}.png: {removed}픽셀 제거")
            img.save(img_path, 'PNG')

if __name__ == "__main__":
    main()
