# TypeCreature (타이핑 다마고치)

타이핑 연습과 귀여운 크리처 수집을 결합한 데스크탑 아이들 게임

---

## 기술 스택

| 분류 | 기술 | 버전 |
|------|------|------|
| **프레임워크** | .NET | 8.0 |
| **UI** | Avalonia UI | 11.3.11 |
| **MVVM** | CommunityToolkit.Mvvm | 8.4.0 |
| **DB** | SQLite (Microsoft.Data.Sqlite) | 10.0.2 |
| **자동 업데이트** | Velopack | 0.0.1015 |
| **이미지 생성** | Python + DALL-E 3 / Gemini Imagen | - |

---

## 프로젝트 구조

```
typing-tamagotchi/
├── TypingTamagotchi/          # 메인 C# 프로젝트
│   ├── Assets/                # 게임 리소스
│   │   ├── Creatures/         # 크리처 스프라이트 (50개)
│   │   ├── Eggs/              # 알 스프라이트 (5종 + 전설)
│   │   ├── Playground/        # 놀이터 배경/이펙트
│   │   └── UI/                # UI 프레임, 받침대
│   ├── Models/                # 데이터 모델
│   ├── Views/                 # Avalonia XAML UI
│   ├── ViewModels/            # MVVM 뷰모델
│   └── Services/              # 비즈니스 로직
├── scripts/                   # 이미지 생성 스크립트
├── docs/                      # 기획 문서
├── .github/workflows/         # GitHub Actions (자동 빌드/배포)
└── typecreature.sln           # Visual Studio 솔루션
```

---

## 데이터베이스 스키마 (SQLite)

### creatures - 크리처 정보 (53종)
```sql
CREATE TABLE creatures (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,              -- 크리처 이름
    rarity INTEGER NOT NULL,         -- 희귀도 (0:Common, 1:Rare, 2:Epic, 3:Legendary)
    element INTEGER NOT NULL,        -- 속성 (0:Water, 1:Fire, 2:Earth, 3:Wind, 4:Lightning)
    sprite_path TEXT NOT NULL,       -- 스프라이트 경로 (Creatures/{id}.png)
    description TEXT NOT NULL,       -- 설명
    age TEXT DEFAULT '',             -- 나이
    gender TEXT DEFAULT '',          -- 성별
    favorite_food TEXT DEFAULT '',   -- 좋아하는 음식
    dislikes TEXT DEFAULT '',        -- 싫어하는 것
    background TEXT DEFAULT ''       -- 배경 스토리
);
```

**희귀도 분포:**
- Common (ID 1-25): 25종
- Rare (ID 26-40): 15종
- Epic (ID 41-47): 7종
- Legendary (ID 48-50): 3종

### collection - 수집한 크리처
```sql
CREATE TABLE collection (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    creature_id INTEGER NOT NULL,    -- 크리처 ID (FK)
    obtained_at TEXT NOT NULL        -- 획득 시간
);
```

### current_egg - 현재 부화중인 알
```sql
CREATE TABLE current_egg (
    id INTEGER PRIMARY KEY CHECK (id = 1),  -- 항상 1개만 존재
    name TEXT NOT NULL,                      -- 알 이름
    sprite_path TEXT NOT NULL,               -- 스프라이트 경로
    required_count INTEGER NOT NULL,         -- 필요 타이핑 수
    current_count INTEGER NOT NULL           -- 현재 타이핑 수
);
```

### display_slots - 진열장 슬롯 (최대 12칸)
```sql
CREATE TABLE display_slots (
    slot_index INTEGER PRIMARY KEY CHECK (slot_index >= 0 AND slot_index < 12),
    creature_id INTEGER NOT NULL     -- 배치된 크리처 ID (FK)
);
```

### playground_creatures - 놀이터 크리처 (최대 6마리)
```sql
CREATE TABLE playground_creatures (
    slot INTEGER PRIMARY KEY CHECK (slot >= 0 AND slot < 6),
    creature_id INTEGER NOT NULL     -- 배치된 크리처 ID (FK)
);
```

### stats - 통계
```sql
CREATE TABLE stats (
    key TEXT PRIMARY KEY,            -- 통계 키
    value INTEGER NOT NULL           -- 값
);
```
**사용 키:** `total_keystrokes` (총 타이핑 수)

### settings - 설정
```sql
CREATE TABLE settings (
    key TEXT PRIMARY KEY,            -- 설정 키
    value TEXT NOT NULL              -- 값
);
```
**사용 키:** `playground_width`, `playground_height`, `first_run` 등

---

## 핵심 기능

### 1. 타이핑 연습
- 전역 키보드 후킹으로 타이핑 감지
- 타이핑 수에 따라 알 부화 진행

### 2. 알 부화 시스템
- 5가지 속성 알 (물, 불, 땅, 바람, 번개) + 전설 알
- 희귀도별 필요 타이핑 수 차등
- 뽑기(슬롯머신) 연출

### 3. 크리처 수집
- 53종 크리처 수집
- 진열장에 최대 12마리 전시
- 도감 기능

### 4. 놀이터
- 최대 6마리 크리처 배치
- 물리 기반 움직임 (Walk, Float, Bounce, Idle 패턴)
- 크리처 간 충돌 인터랙션
- 창 크기 조절 가능

### 5. 미니 위젯
- 항상 위에 표시되는 작은 창
- 현재 시간, 타이핑 수, 알 부화 진행률 표시
- 접기/펼치기 기능

---

## 이미지 생성 스크립트

| 스크립트 | 용도 |
|---------|------|
| `generate_assets.py` | DALL-E 3로 크리처/알 생성 |
| `generate_assets_gemini.py` | Gemini Imagen으로 생성 |
| `generate_legendary_egg.py` | 전설 알 생성 |
| `generate_playground_assets.py` | 놀이터 배경 생성 |
| `generate_playground_bg_batch.py` | 배경 일괄 생성 |
| `regenerate_eggs.py` | 알 재생성 |
| `regenerate_kelpie.py` | 특정 크리처 재생성 |

**필요 환경변수:**
- `OPENAI_API_KEY` - DALL-E 사용 시
- `GEMINI_API_KEY` - Gemini 사용 시

---

## 빌드 및 실행

```bash
# 개발 빌드
cd TypingTamagotchi
dotnet build

# 실행
dotnet run

# 릴리즈 빌드 (Windows x64)
dotnet publish -c Release -r win-x64 --self-contained
```

---

## 배포 (GitHub Actions + Velopack)

### 자동 업데이트 시스템
- **Velopack** 사용 - GitHub Releases 기반 자동 업데이트
- 설치 경로: `%LocalAppData%\TypingTamagotchi`
- 관리자 권한 불필요

### 일반 개발 (코드만 푸시)
```bash
git add .
git commit -m "메시지"
git push
```
→ 릴리스 생성 안됨, 코드만 GitHub에 올라감

### 새 버전 배포 (릴리스 생성)
```bash
# 1. 변경사항 커밋 & 푸시
git add .
git commit -m "feat: 새 기능"
git push

# 2. 버전 태그 생성 & 푸시
git tag v1.0.1
git push origin v1.0.1
```
→ GitHub Actions 자동 실행 → 릴리스 생성

### 버전 규칙 (Semantic Versioning)
| 변경 | 버전 예시 | 설명 |
|------|-----------|------|
| 버그 수정 | v1.0.0 → v1.0.1 | 패치 버전 증가 |
| 기능 추가 | v1.0.1 → v1.1.0 | 마이너 버전 증가 |
| 큰 변경 | v1.1.0 → v2.0.0 | 메이저 버전 증가 |

### 배포 결과물
GitHub Releases에 자동 업로드:
- `Setup.exe` - 설치 프로그램 (사용자 배포용)
- `RELEASES` - 업데이트 메타데이터
- `TypingTamagotchi-win-x64.nupkg` - 델타 업데이트 패키지

### 사용자 경험
1. **첫 설치**: `Setup.exe` 다운로드 → 실행
2. **이후 업데이트**: 앱 실행 시 자동 체크 → 상단 알림 → 업데이트 버튼 클릭

---

## 크리처 목록 (53종)

### Common (25종) - ID 1~25
| ID | 이름 | 속성 | 설명 |
|----|------|------|------|
| 1 | 슬라임 | Water | 말랑말랑한 젤리 생물 |
| 2 | 꼬마구름 | Wind | 둥실둥실 떠다니는 구름 |
| 3 | 잎새 | Earth | 바람에 흔들리는 잎사귀 |
| 4 | 물방울 | Water | 투명하게 빛나는 물방울 |
| 5 | 돌멩이 | Earth | 단단한 작은 돌 |
| 6 | 별똥별 | Fire | 하늘에서 떨어진 작은 별 |
| 7 | 꽃잎 | Earth | 향기로운 분홍 꽃잎 |
| 8 | 솜뭉치 | Wind | 폭신폭신한 솜 |
| 9 | 젤리콩 | Lightning | 달콤한 젤리 콩 |
| 10 | 이끼돌 | Earth | 이끼가 낀 귀여운 돌 |
| 11 | 눈송이 | Water | 차가운 눈 결정 |
| 12 | 반딧불 | Fire | 밤에 빛나는 벌레 |
| 13 | 씨앗 | Earth | 가능성이 담긴 씨앗 |
| 14 | 조약돌 | Earth | 강에서 온 매끈한 돌 |
| 15 | 먼지토끼 | Wind | 뽀송뽀송한 먼지 덩어리 |
| 16 | 비누방울 | Water | 무지개빛 비누방울 |
| 17 | 도토리 | Earth | 다람쥐가 좋아하는 열매 |
| 18 | 꿀방울 | Fire | 달콤한 황금 방울 |
| 19 | 깃털 | Wind | 가벼운 새 깃털 |
| 20 | 이슬 | Water | 아침에 맺힌 이슬 |
| 21 | 모래알 | Earth | 해변의 작은 모래 |
| 22 | 풀잎 | Earth | 초록빛 풀잎 |
| 23 | 나뭇가지 | Earth | 작은 나무 조각 |
| 24 | 진흙이 | Earth | 말랑한 진흙 덩어리 |
| 25 | 버섯 | Earth | 동글동글한 버섯 |

### Rare (15종) - ID 26~40
| ID | 이름 | 속성 | 설명 |
|----|------|------|------|
| 26 | 번개토끼 | Lightning | 전기를 품은 토끼 |
| 27 | 불꽃여우 | Fire | 꼬리에서 불꽃이 피는 여우 |
| 28 | 얼음펭귄 | Water | 차가운 기운의 펭귄 |
| 29 | 바람새 | Wind | 바람을 타고 나는 새 |
| 30 | 꽃사슴 | Earth | 뿔에 꽃이 피는 사슴 |
| 31 | 달토끼 | Lightning | 달빛을 받으면 빛나는 토끼 |
| 32 | 무지개뱀 | Lightning | 일곱 색깔 비늘의 뱀 |
| 33 | 구름고래 | Water | 하늘을 헤엄치는 고래 |
| 34 | 수정나비 | Lightning | 투명한 날개의 나비 |
| 35 | 숲요정 | Earth | 숲을 지키는 작은 요정 |
| 36 | 별똥곰 | Lightning | 별빛 털을 가진 곰 |
| 37 | 파도물개 | Water | 파도를 타는 물개 |
| 38 | 안개늑대 | Wind | 안개 속에서 나타나는 늑대 |
| 39 | 노을새 | Fire | 저녁노을 빛깔의 새 |
| 40 | 이끼거북 | Earth | 등에 정원이 있는 거북 |

### Epic (7종) - ID 41~47
| ID | 이름 | 속성 | 설명 |
|----|------|------|------|
| 41 | 용아기 | Fire | 아직 어린 용 |
| 42 | 유니콘 | Lightning | 무지개 갈기의 유니콘 |
| 43 | 피닉스 | Fire | 불꽃에서 다시 태어나는 새 |
| 44 | 크라켄 | Water | 심해의 거대 문어 |
| 45 | 그리폰 | Wind | 독수리와 사자의 합체 |
| 46 | 켈피 | Water | 물속의 신비한 말 |
| 47 | 바실리스크 | Earth | 눈빛이 무서운 뱀 |

### Legendary (5종) - ID 48~50, 52~53
| ID | 이름 | 속성 | 설명 |
|----|------|------|------|
| 48 | 황금드래곤 | Fire | 전설의 황금빛 용 |
| 49 | 세계수정령 | Earth | 세계수를 지키는 정령 |
| 50 | 시간고양이 | Lightning | 시간을 다루는 신비한 고양이 |
| 52 | 네시 | Water | 스코틀랜드 호수의 전설 |
| 53 | 빅풋 | Earth | 숲속의 거대한 발자국 주인 |

### 추가 Common (1종) - ID 51
| ID | 이름 | 속성 | 설명 |
|----|------|------|------|
| 51 | 지우개똥 | Earth | 말랑말랑한 지우개 모양 생물 |

---

## 라이선스

MIT License
