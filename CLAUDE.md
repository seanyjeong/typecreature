# Typing Tamagotchi

타이핑으로 크리처를 수집하는 데스크톱 펫 게임

## 프로젝트 정보

| 항목 | 값 |
|------|-----|
| **현재 버전** | 1.2.27 |
| **스택** | .NET 8 + Avalonia UI 11.3.11 |
| **DB** | SQLite (Microsoft.Data.Sqlite) |
| **패턴** | MVVM (CommunityToolkit.Mvvm) |
| **자동 업데이트** | Velopack |
| **배포** | GitHub Releases |

## 주요 기능

### 크리처 시스템
- 54종 크리처 (Common/Rare/Epic/Legendary)
- 5가지 속성: Fire, Water, Earth, Wind, Lightning
- 알 부화 시스템 (타이핑/클릭/시간으로 게이지 충전)

### 타이핑 연습 (v1.2.26)
- 한글 속담 200개 + 영어 속담 100개
- 한/영 토글 버튼
- 실시간 CPM (Stopwatch 기반 정확 측정)
- 문장 완료 시 Enter로 다음 문장 이동
- 종료 시 결과 요약 팝업
- 부화 기여도 퍼센트 표시

### 놀이터
- 최대 5마리 크리처 배치
- 자연스러운 충돌 처리 (겹치지 않고 튕김)
- 크리처별 스프라이트 방향 자동 처리

### 미니 위젯
- 시스템 트레이 상주
- 작업 중에도 크리처 확인 가능

## 폴더 구조

```
TypingTamagotchi/
├── Assets/
│   ├── Creatures/           # 크리처 원본 이미지 (1024x1024)
│   │   └── thumbs/          # 썸네일 (256x256, 메모리 최적화)
│   ├── typing_sentences.json     # 한글 속담
│   ├── typing_sentences_en.json  # 영어 속담
│   └── changelog.json       # 버전 히스토리
├── Models/
│   ├── Creature.cs
│   ├── Egg.cs
│   ├── Rarity.cs
│   └── Element.cs
├── ViewModels/
│   ├── MainWindowViewModel.cs
│   ├── TypingPracticeViewModel.cs
│   └── Converters.cs
├── Views/
│   ├── MainWindow.axaml
│   ├── TypingPracticeWindow.axaml
│   └── PlaygroundWindow.axaml
└── Services/
    ├── DatabaseService.cs    # SQLite 연동
    ├── HatchingService.cs    # 부화 로직
    ├── UpdateService.cs      # Velopack 자동 업데이트
    └── ImageCacheService.cs  # 썸네일 캐싱 (메모리 최적화)
```

## 빌드 & 배포

```bash
# 로컬 빌드
dotnet build

# 릴리스 빌드
dotnet publish -c Release -o publish -r win-x64 --self-contained true

# 새 버전 릴리스
# 1. csproj 버전 업데이트
# 2. changelog.json 업데이트
# 3. git commit & push
# 4. git tag v1.x.x && git push origin v1.x.x
```

## 최근 업데이트

| 버전 | 주요 변경 |
|------|----------|
| 1.2.27 | 평균 타수 버그 수정 (대기 시간 제외, 부화 시 멈춤) |
| 1.2.26 | 영문 타이핑, Enter 확인, 결과 팝업, 부화 기여% |
| 1.2.25 | 메모리 최적화 (썸네일 캐싱, ~200MB → ~15MB) |
| 1.2.24 | 용민이의 선물 - 렌고쿠 자동 지급 |
| 1.2.23 | 새 크리처: 렌고쿠 (Epic/Fire) |
| 1.2.22 | 타수 표시 개선 (문장 시작 시 초기화) |

## DB 스키마

```sql
-- 크리처 정보
creatures (id, name, rarity, element, sprite_path, description, ...)

-- 수집한 크리처
collection (id, creature_id, obtained_at)

-- 현재 부화 중인 알
current_egg (id, name, sprite_path, required_count, current_count)

-- 놀이터 배치
playground_creatures (slot, creature_id)

-- 설정
settings (key, value)
```

## 참고

- 크리처 이미지: 1024x1024 PNG (투명 배경)
- 썸네일: 256x256 PNG (UI 표시용)
- 자동 업데이트: 1시간 간격 체크 + 앱 시작 시 체크
