#!/usr/bin/env python3
"""
하단 텍스트 강제 제거
하단 영역에서 크리처와 연결되지 않은 모든 픽셀 제거
"""

from PIL import Image
from pathlib import Path
from collections import deque

def find_main_creature(img):
    """크리처의 메인 영역을 찾기 (가장 큰 연결 컴포넌트)"""
    img = img.convert('RGBA')
    width, height = img.size
    pixels = img.load()

    visited = [[False] * height for _ in range(width)]
    components = []

    def bfs(start_x, start_y):
        """BFS로 연결된 픽셀 찾기"""
        queue = deque([(start_x, start_y)])
        component = []

        while queue:
            x, y = queue.popleft()

            if x < 0 or x >= width or y < 0 or y >= height:
                continue
            if visited[x][y]:
                continue

            pixel = pixels[x, y]
            if pixel[3] == 0:  # 투명
                continue

            visited[x][y] = True
            component.append((x, y))

            # 8방향
            for dx in [-1, 0, 1]:
                for dy in [-1, 0, 1]:
                    if dx != 0 or dy != 0:
                        queue.append((x + dx, y + dy))

        return component

    # 모든 불투명 픽셀에서 컴포넌트 찾기
    for y in range(height):
        for x in range(width):
            if not visited[x][y] and pixels[x, y][3] > 0:
                component = bfs(x, y)
                if component:
                    components.append(component)

    return components

def remove_small_components(img, min_ratio=0.01):
    """작은 컴포넌트들 제거 (워터마크 등)"""
    img = img.convert('RGBA')
    width, height = img.size
    pixels = img.load()

    components = find_main_creature(img)

    if not components:
        return img, 0

    # 가장 큰 컴포넌트 찾기
    main_component = max(components, key=len)
    main_size = len(main_component)

    removed = 0

    # 작은 컴포넌트들 제거
    for component in components:
        if len(component) < main_size * min_ratio:
            # 이 컴포넌트가 하단에 있으면 제거 (워터마크일 가능성)
            avg_y = sum(p[1] for p in component) / len(component)
            if avg_y > height * 0.7:  # 하단 30%에 있으면
                for x, y in component:
                    pixels[x, y] = (0, 0, 0, 0)
                    removed += 1

    return img, removed

def process_image(img_path):
    """이미지 처리"""
    try:
        img = Image.open(img_path)
        img, removed = remove_small_components(img, min_ratio=0.005)

        if removed > 0:
            print(f"  {img_path.name}: {removed}픽셀 제거")
            img.save(img_path, 'PNG')

        return removed

    except Exception as e:
        print(f"  {img_path.name}: 에러 - {e}")
        return 0

def main():
    base_dir = Path(__file__).parent.parent / "TypingTamagotchi" / "Assets"

    print("=" * 50)
    print("하단 작은 컴포넌트(워터마크) 제거")
    print("=" * 50)

    total = 0

    for folder in ["Creatures", "Eggs"]:
        folder_path = base_dir / folder
        print(f"\n{folder}:")
        for png in sorted(folder_path.glob("*.png")):
            removed = process_image(png)
            total += removed

    print(f"\n총 {total}픽셀 제거됨")

if __name__ == "__main__":
    main()
