# Typing Tamagotchi

타이핑으로 크리처를 수집하는 데스크톱 펫 게임

## 프로젝트 정보

| 항목 | 값 |
|------|-----|
| **현재 버전** | 1.2.31 |
| **스택** | .NET 8 + Avalonia UI 11.3.11 |
| **DB** | SQLite (Microsoft.Data.Sqlite) |
| **패턴** | MVVM (CommunityToolkit.Mvvm) |
| **자동 업데이트** | Velopack |
| **배포** | GitHub Releases (GitHub Actions) |
| **저장소** | github.com/seanyjeong/typecreature |

## 주요 기능

### 크리처 시스템
- 54종 크리처 (Common/Rare/Epic/Legendary)
- 5가지 속성: Fire, Water, Earth, Wind, Lightning
- 알 부화 시스템 (타이핑/클릭/시간으로 게이지 충전)
- 도감에서 크리처 클릭 시 상세정보 팝업

### 타이핑 연습
- **한글**: 속담 200개
- **영어**: 격언 100개 + JS/TS 코딩 100개 (토글)
- 현재 타수 / 최고 타수 / 평균 타수 / 정확도
- 엔터로 다음 문장 이동 (정확도 무관)
- 부화 기여도 표시

### 놀이터
- 최대 6마리 크리처 배치
- 자연스러운 충돌 처리

### 미니 위젯
- 시스템 트레이 상주
- 부화 게이지 실시간 표시

## 폴더 구조

```
TypingTamagotchi/
├── Assets/
│   ├── Creatures/                  # 크리처 이미지 (1024x1024)
│   │   └── thumbs/                 # 썸네일 (256x256)
│   ├── typing_sentences.json       # 한글 속담
│   ├── typing_sentences_en.json    # 영어 격언
│   ├── typing_sentences_code.json  # JS/TS 코드
│   └── changelog.json              # 버전 히스토리
├── Models/
│   ├── Creature.cs, Egg.cs, Rarity.cs, Element.cs
│   └── PlaygroundCreature.cs, DesktopPet.cs
├── ViewModels/
│   ├── MainWindowViewModel.cs
│   ├── TypingPracticeViewModel.cs
│   ├── CollectionViewModel.cs
│   ├── PlaygroundViewModel.cs
│   ├── MiniWidgetViewModel.cs
│   └── Converters.cs
├── Views/
│   ├── MainWindow.axaml
│   ├── TypingPracticeWindow.axaml
│   ├── CollectionWindow.axaml
│   ├── PlaygroundWindow.axaml
│   └── MiniWidget.axaml
└── Services/
    ├── DatabaseService.cs      # SQLite
    ├── HatchingService.cs      # 부화 로직
    ├── EggService.cs           # 알 관리
    ├── UpdateService.cs        # Velopack 자동 업데이트
    ├── ChangelogService.cs     # 변경 로그
    └── ImageCacheService.cs    # 썸네일 캐싱
```

## 빌드 & 배포

```bash
# 로컬 빌드
dotnet build

# 새 버전 릴리스 (GitHub Actions 자동 빌드)
# 1. csproj 버전 업데이트
# 2. changelog.json 업데이트
# 3. git commit & push
# 4. git tag v1.x.x && git push origin v1.x.x
```

## 최근 업데이트

| 버전 | 주요 변경 |
|------|----------|
| 1.2.31 | 엔터로 다음 문장 (정확도 무관), 도감 팝업 UI 개선 |
| 1.2.30 | 도감 크리처 클릭 시 상세정보 팝업 |
| 1.2.29 | 최고 타수 문장 완료 시에만 업데이트 |
| 1.2.28 | 세션 최고 타수, JS/TS 코딩 연습 |
| 1.2.27 | 평균 타수 버그 수정 (대기 시간 제외) |
| 1.2.26 | 영문 타이핑, Enter 확인, 결과 팝업 |
| 1.2.25 | 메모리 최적화 (~200MB → ~15MB) |

## DB 스키마

```sql
creatures (id, name, rarity, element, sprite_path, description, age, gender, favorite_food, dislikes, background)
collection (id, creature_id, obtained_at)
stats (key, value)  -- keystrokes, clicks
playground_creatures (slot, creature_id)
display_slots (slot, creature_id)
settings (key, value)
```

## 참고

- 크리처 이미지: 1024x1024 PNG (투명 배경)
- 썸네일: 256x256 PNG (UI 표시용)
- 자동 업데이트: 앱 시작 시 + 1시간 간격 체크
- 이미지 생성: Gemini API (API 키 사용, 구독과 별도 과금)
