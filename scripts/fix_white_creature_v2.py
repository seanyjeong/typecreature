#!/usr/bin/env python3
"""
흰색 크리처 전용 배경 제거 v2
외곽선을 경계로 바깥쪽만 제거
"""

from PIL import Image
from pathlib import Path
from collections import deque

def is_outline_color(r, g, b):
    """외곽선 색상인지 (검은색/어두운 색)"""
    return r < 80 and g < 80 and b < 80

def is_background_color(r, g, b):
    """배경 색상인지 (체크무늬 흰색/회색)"""
    # 균일한 회색 또는 흰색
    is_uniform = abs(r - g) < 20 and abs(g - b) < 20
    is_light = r >= 180 and g >= 180 and b >= 180
    return is_uniform and is_light

def remove_background_from_edges(img):
    """가장자리에서 시작해서 외곽선에 닿을 때까지 제거"""
    img = img.convert('RGBA')
    width, height = img.size
    pixels = img.load()

    # 방문 체크
    visited = [[False] * height for _ in range(width)]
    to_remove = set()

    def bfs_from_edge(start_x, start_y):
        queue = deque([(start_x, start_y)])

        while queue:
            x, y = queue.popleft()

            if x < 0 or x >= width or y < 0 or y >= height:
                continue
            if visited[x][y]:
                continue

            visited[x][y] = True

            p = pixels[x, y]
            if p[3] == 0:  # 이미 투명
                for dx, dy in [(-1,0), (1,0), (0,-1), (0,1), (-1,-1), (-1,1), (1,-1), (1,1)]:
                    queue.append((x + dx, y + dy))
                continue

            r, g, b = p[0], p[1], p[2]

            # 외곽선이면 멈춤 (크리처 경계)
            if is_outline_color(r, g, b):
                continue

            # 배경색이면 제거 대상에 추가하고 계속 탐색
            if is_background_color(r, g, b):
                to_remove.add((x, y))
                for dx, dy in [(-1,0), (1,0), (0,-1), (0,1), (-1,-1), (-1,1), (1,-1), (1,1)]:
                    queue.append((x + dx, y + dy))

    # 상단/하단 가장자리에서 시작
    for x in range(width):
        bfs_from_edge(x, 0)
        bfs_from_edge(x, height - 1)

    # 좌측/우측 가장자리에서 시작
    for y in range(height):
        bfs_from_edge(0, y)
        bfs_from_edge(width - 1, y)

    # 제거
    for x, y in to_remove:
        pixels[x, y] = (0, 0, 0, 0)

    # 하단 워터마크 텍스트 제거 (어두운 색)
    watermark_removed = 0
    for y in range(height - 70, height):
        for x in range(width):
            p = pixels[x, y]
            if p[3] == 0:
                continue
            r, g, b = p[0], p[1], p[2]
            # 어두운 텍스트 색상
            if r < 100 and g < 100 and b < 120:
                # 주변에 투명 픽셀이 많으면 워터마크
                trans_count = 0
                for dy in range(-2, 3):
                    for dx in range(-2, 3):
                        nx, ny = x + dx, y + dy
                        if 0 <= nx < width and 0 <= ny < height:
                            if pixels[nx, ny][3] == 0:
                                trans_count += 1
                if trans_count > 5:
                    pixels[x, y] = (0, 0, 0, 0)
                    watermark_removed += 1

    return img, len(to_remove), watermark_removed

def process_image(img_path):
    """이미지 처리"""
    try:
        img = Image.open(img_path)
        img, bg_removed, wm_removed = remove_background_from_edges(img)

        print(f"  {img_path.name}: 배경 {bg_removed}픽셀, 워터마크 {wm_removed}픽셀 제거")
        img.save(img_path, 'PNG')
        return True

    except Exception as e:
        print(f"  {img_path.name}: 에러 - {e}")
        return False

def main():
    import sys

    if len(sys.argv) < 2:
        print("사용법: python fix_white_creature_v2.py <이미지번호>")
        return

    base_dir = Path(__file__).parent.parent / "TypingTamagotchi" / "Assets" / "Creatures"

    for num in sys.argv[1:]:
        # 먼저 git에서 원본 복원
        img_path = base_dir / f"{num}.png"
        import subprocess
        subprocess.run(f"git show HEAD~1:TypingTamagotchi/Assets/Creatures/{num}.png > /tmp/{num}_orig.png && cp /tmp/{num}_orig.png {img_path}", shell=True)

        if img_path.exists():
            print(f"처리 중: {img_path}")
            process_image(img_path)

if __name__ == "__main__":
    main()
