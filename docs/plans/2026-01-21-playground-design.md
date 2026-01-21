# 놀이터 (Playground) 기능 설계

## 개요
도감에서 크리처를 "놀러 보내기"하면 별도 윈도우에서 통통 뛰어다니는 기능.

## 핵심 규칙
- **진열장 vs 놀이터 상호 배타적**: 진열장에 있으면 놀이터 못 감, 놀이터에 있으면 진열장 못 감
- **최대 4마리** 동시에 놀이터에 있을 수 있음

## 놀이터 윈도우
- **별도 윈도우** (PlaygroundWindow)
- **크기**: 400x150px (가로로 긴 직사각형)
- **스타일**: 투명 배경, 테두리 없음, 항상 위에, 드래그 이동 가능
- **배경**: 잔디밭 픽셀아트 (AI 생성)

## 애니메이션
### 움직임 (단일 이미지 기반)
- **Y 위치만 이동** (점프) - 사인파 형태
- **바닥 그림자** - 타원형, 점프 높이에 따라 크기 변화
- **좌우 이동** - X 위치 변경, 방향 바뀔 때 ScaleX: -1로 flip
- **이미지 변형 없음** - 찌그러짐(squash/stretch) 사용 안 함

### 충돌 (2가지 케이스)
**1. 옆에서 부딪힘**
- 둘 다 **90도 회전** (옆으로 넘어짐)
- 충돌 지점에 **"쿵!" 이펙트** 표시
- 1-2초 후 복귀

**2. 위에서 밟음 (머리 위로 착지)**
- 밟힌 애: **ScaleY: 0.8**로 살짝 납작해짐 + "쿵!" 이펙트
- 밟은 애: 튀어오름 (점프 보너스)
- 0.3초 후 복귀
- 점프로 다른 크리처를 넘어갈 수도 있음

### 게임 루프
- 16ms 간격 DispatcherTimer (약 60fps)
- 위치 업데이트 → 충돌 체크 → 이펙트 트리거

## DB 변경
```sql
CREATE TABLE playground_creatures (
    slot INTEGER PRIMARY KEY,  -- 0, 1, 2, 3
    creature_id INTEGER NOT NULL
);
```

## 파일 구조
```
Views/
  PlaygroundWindow.axaml
  PlaygroundWindow.axaml.cs

ViewModels/
  PlaygroundViewModel.cs

Models/
  PlaygroundCreature.cs

Assets/
  Playground/
    background.png      # 잔디밭 배경 400x150
    bump_effect.png     # "쿵!" 이펙트 64x64
```

## 도감 UI 변경
- "진열장에 추가" 버튼 옆에 "놀러 보내기" 버튼 추가
- 상태에 따라 버튼 비활성화 처리

## 이미지 렌더링
```xml
RenderOptions.BitmapInterpolationMode="NearestNeighbor"
```
픽셀아트 샤프하게 유지

## 생성할 에셋
1. 잔디밭 배경 (400x150, 픽셀아트)
2. "쿵!" 이펙트 (64x64, 만화 스타일)
