#!/usr/bin/env python3
"""
메인 크리처가 아닌 분리된 컴포넌트 제거
(워터마크 배지 등)
"""

from PIL import Image
from pathlib import Path
from collections import deque

def find_components(img):
    """모든 연결된 컴포넌트 찾기"""
    img = img.convert('RGBA')
    width, height = img.size
    pixels = img.load()

    visited = [[False] * height for _ in range(width)]
    components = []

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

            for dx, dy in [(-1,0), (1,0), (0,-1), (0,1), (-1,-1), (-1,1), (1,-1), (1,1)]:
                queue.append((x + dx, y + dy))

        return component

    for y in range(height):
        for x in range(width):
            if not visited[x][y] and pixels[x, y][3] > 0:
                component = bfs(x, y)
                if component:
                    components.append(component)

    return components, pixels

def remove_non_main_components(img):
    """가장 큰 컴포넌트(메인 크리처)를 제외한 나머지 제거"""
    components, pixels = find_components(img)

    if not components:
        return img, 0

    # 가장 큰 컴포넌트 = 메인 크리처
    main_component = max(components, key=len)
    main_set = set(main_component)

    removed = 0
    for component in components:
        if component is not main_component:
            for x, y in component:
                pixels[x, y] = (0, 0, 0, 0)
                removed += 1

    return img, removed

def main():
    import sys

    if len(sys.argv) < 2:
        print("사용법: python remove_isolated_components.py <이미지번호>")
        return

    base_dir = Path(__file__).parent.parent / "TypingTamagotchi" / "Assets" / "Creatures"

    for num in sys.argv[1:]:
        img_path = base_dir / f"{num}.png"
        if img_path.exists():
            img = Image.open(img_path)
            img, removed = remove_non_main_components(img)
            print(f"{num}.png: {removed}픽셀 제거")
            img.save(img_path, 'PNG')

if __name__ == "__main__":
    main()
