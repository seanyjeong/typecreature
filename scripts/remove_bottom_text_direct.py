#!/usr/bin/env python3
"""
하단 텍스트 직접 제거
"""

from PIL import Image
from pathlib import Path
from collections import deque

def remove_text_at_bottom(img, scan_height=80):
    """하단 영역에서 크리처와 분리된 텍스트 제거"""
    img = img.convert('RGBA')
    width, height = img.size
    pixels = img.load()

    # 하단 영역에서 불투명 픽셀 컴포넌트 찾기
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

            for dx, dy in [(-1,0), (1,0), (0,-1), (0,1)]:
                queue.append((x + dx, y + dy))

        return component

    # 하단 80픽셀 스캔
    for y in range(height - scan_height, height):
        for x in range(width):
            if not visited[x][y] and pixels[x, y][3] > 0:
                component = bfs(x, y)
                if component:
                    # 이 컴포넌트가 상단까지 연결되어 있는지 확인
                    min_y = min(p[1] for p in component)
                    if min_y > height - scan_height - 20:  # 상단과 연결 안됨 = 텍스트
                        components.append(component)

    # 분리된 컴포넌트(텍스트) 제거
    removed = 0
    for component in components:
        for x, y in component:
            pixels[x, y] = (0, 0, 0, 0)
            removed += 1

    return img, removed

def main():
    import sys

    if len(sys.argv) < 2:
        print("사용법: python remove_bottom_text_direct.py <이미지번호>")
        return

    base_dir = Path(__file__).parent.parent / "TypingTamagotchi" / "Assets" / "Creatures"

    for num in sys.argv[1:]:
        img_path = base_dir / f"{num}.png"
        if img_path.exists():
            img = Image.open(img_path)
            img, removed = remove_text_at_bottom(img)
            print(f"{num}.png: {removed}픽셀 제거")
            img.save(img_path, 'PNG')

if __name__ == "__main__":
    main()
