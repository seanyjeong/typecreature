# TypeCreature (타이핑 다마고치)

타이핑과 마우스 클릭으로 알을 부화시켜 귀여운 크리처를 수집하는 데스크톱 아이들러 게임

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)
![Avalonia](https://img.shields.io/badge/Avalonia-11-blue.svg)

## 스크린샷

> 🚧 Coming Soon - 픽셀아트 에셋 작업 중

## 특징

- **입력 트래킹**: 키보드 타이핑, 마우스 클릭 횟수 카운트 (전역)
- **알 부화**: 랜덤 게이지 (500~2000) 도달 시 부화
- **크리처 수집**: 50종 크리처 도감
- **희귀도 시스템**: Common (50%) / Rare (30%) / Epic (15%) / Legendary (5%)
- **미니 위젯**: 화면 구석에 떠있는 게이지 표시
- **시스템 트레이**: 백그라운드 실행 지원

## 기술 스택

- **.NET 8**
- **Avalonia UI 11** - 크로스플랫폼 UI 프레임워크
- **SQLite** - 로컬 데이터 저장
- **CommunityToolkit.Mvvm** - MVVM 패턴

## 설치 및 실행

### 요구사항

- .NET 8 SDK
- Windows / Linux / macOS

### 빌드

```bash
git clone https://github.com/seanyjeong/typecreature.git
cd typecreature/TypingTamagotchi
dotnet build
dotnet run
```

## 프로젝트 구조

```
TypingTamagotchi/
├── Models/
│   ├── Rarity.cs          # 희귀도 enum
│   ├── Creature.cs        # 크리처 모델
│   ├── Egg.cs             # 알 모델
│   └── CollectionEntry.cs # 수집 기록
├── Services/
│   ├── DatabaseService.cs      # SQLite DB
│   ├── EggService.cs           # 알 관리
│   ├── HatchingService.cs      # 부화 로직
│   ├── IInputService.cs        # 입력 인터페이스
│   ├── SimulatedInputService.cs # 테스트용
│   ├── WindowsInputService.cs  # Windows 전역 후킹
│   └── InputServiceFactory.cs  # 플랫폼별 선택
├── ViewModels/
│   ├── MainWindowViewModel.cs
│   ├── CollectionViewModel.cs
│   └── Converters.cs
├── Views/
│   ├── MainWindow.axaml        # 메인 화면
│   ├── CollectionWindow.axaml  # 도감
│   └── MiniWidget.axaml        # 미니 위젯
└── Assets/
    ├── Eggs/       # 알 이미지
    └── Creatures/  # 크리처 이미지 (50종)
```

## 크리처 목록

### Legendary (3종)
- 황금드래곤, 세계수정령, 시간고양이

### Epic (7종)
- 용아기, 유니콘, 피닉스, 크라켄, 그리폰, 켈피, 바실리스크

### Rare (15종)
- 번개토끼, 불꽃여우, 얼음펭귄, 바람새, 꽃사슴, 달토끼, 무지개뱀, 구름고래, 수정나비, 숲요정, 별똥곰, 파도물개, 안개늑대, 노을새, 이끼거북

### Common (25종)
- 슬라임, 꼬마구름, 잎새, 물방울, 돌멩이, 별똥별, 꽃잎, 솜뭉치, 젤리콩, 이끼돌, 눈송이, 반딧불, 씨앗, 조약돌, 먼지토끼, 비누방울, 도토리, 꿀방울, 깃털, 이슬, 모래알, 풀잎, 나뭇가지, 진흙이, 버섯

## 라이선스

MIT License

## 크레딧

- 개발: Claude Code + Sean
- 아이디어: 다마고치, Cookie Clicker, 포켓몬 도감
