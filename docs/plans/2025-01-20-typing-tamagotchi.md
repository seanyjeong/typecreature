# Typing Tamagotchi Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 타이핑/마우스 클릭으로 알을 부화시켜 크리처를 수집하는 데스크톱 아이들러 게임

**Architecture:** Avalonia MVVM 패턴. Services 레이어에서 비즈니스 로직 처리, SQLite로 데이터 저장, 전역 입력 후킹으로 키보드/마우스 감지

**Tech Stack:** .NET 8, Avalonia UI 11, SQLite (Microsoft.Data.Sqlite), CommunityToolkit.Mvvm

---

## Phase 1: 데이터 모델 및 DB

### Task 1: NuGet 패키지 추가

**Files:**
- Modify: `TypingTamagotchi/TypingTamagotchi.csproj`

**Step 1: 필요한 패키지 추가**

```bash
cd ~/typing-tamagotchi/TypingTamagotchi
dotnet add package Microsoft.Data.Sqlite
dotnet add package CommunityToolkit.Mvvm
```

**Step 2: 빌드 확인**

Run: `dotnet build`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add -A
git commit -m "chore: add SQLite and MVVM toolkit packages"
```

---

### Task 2: Rarity Enum 생성

**Files:**
- Create: `TypingTamagotchi/Models/Rarity.cs`

**Step 1: Rarity enum 작성**

```csharp
namespace TypingTamagotchi.Models;

public enum Rarity
{
    Common,     // 50%
    Rare,       // 30%
    Epic,       // 15%
    Legendary   // 5%
}
```

**Step 2: 빌드 확인**

Run: `dotnet build`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add -A
git commit -m "feat: add Rarity enum"
```

---

### Task 3: Creature 모델 생성

**Files:**
- Create: `TypingTamagotchi/Models/Creature.cs`

**Step 1: Creature 클래스 작성**

```csharp
namespace TypingTamagotchi.Models;

public class Creature
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Rarity Rarity { get; set; }
    public string SpritePath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
```

**Step 2: 빌드 확인**

Run: `dotnet build`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add -A
git commit -m "feat: add Creature model"
```

---

### Task 4: Egg 모델 생성

**Files:**
- Create: `TypingTamagotchi/Models/Egg.cs`

**Step 1: Egg 클래스 작성**

```csharp
namespace TypingTamagotchi.Models;

public class Egg
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SpritePath { get; set; } = string.Empty;
    public int RequiredCount { get; set; }  // 500~2000 랜덤
    public int CurrentCount { get; set; }

    public double Progress => RequiredCount > 0
        ? (double)CurrentCount / RequiredCount
        : 0;

    public bool IsReady => CurrentCount >= RequiredCount;
}
```

**Step 2: 빌드 확인**

Run: `dotnet build`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add -A
git commit -m "feat: add Egg model"
```

---

### Task 5: CollectionEntry 모델 생성

**Files:**
- Create: `TypingTamagotchi/Models/CollectionEntry.cs`

**Step 1: CollectionEntry 클래스 작성 (유저가 획득한 크리처 기록)**

```csharp
namespace TypingTamagotchi.Models;

public class CollectionEntry
{
    public int Id { get; set; }
    public int CreatureId { get; set; }
    public DateTime ObtainedAt { get; set; }
}
```

**Step 2: 빌드 확인**

Run: `dotnet build`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add -A
git commit -m "feat: add CollectionEntry model"
```

---

### Task 6: DatabaseService 생성

**Files:**
- Create: `TypingTamagotchi/Services/DatabaseService.cs`

**Step 1: DatabaseService 작성**

```csharp
using Microsoft.Data.Sqlite;
using TypingTamagotchi.Models;

namespace TypingTamagotchi.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string dbPath = "tamagotchi.db")
    {
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS creatures (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                rarity INTEGER NOT NULL,
                sprite_path TEXT NOT NULL,
                description TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS collection (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                creature_id INTEGER NOT NULL,
                obtained_at TEXT NOT NULL,
                FOREIGN KEY (creature_id) REFERENCES creatures(id)
            );

            CREATE TABLE IF NOT EXISTS stats (
                key TEXT PRIMARY KEY,
                value INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS current_egg (
                id INTEGER PRIMARY KEY CHECK (id = 1),
                name TEXT NOT NULL,
                sprite_path TEXT NOT NULL,
                required_count INTEGER NOT NULL,
                current_count INTEGER NOT NULL
            );
        ";
        command.ExecuteNonQuery();
    }

    public SqliteConnection GetConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
```

**Step 2: 빌드 확인**

Run: `dotnet build`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add -A
git commit -m "feat: add DatabaseService with schema"
```

---

### Task 7: 초기 크리처 데이터 시딩

**Files:**
- Modify: `TypingTamagotchi/Services/DatabaseService.cs`

**Step 1: SeedCreatures 메서드 추가**

DatabaseService 클래스에 추가:

```csharp
public void SeedCreaturesIfEmpty()
{
    using var connection = GetConnection();

    var checkCommand = connection.CreateCommand();
    checkCommand.CommandText = "SELECT COUNT(*) FROM creatures";
    var count = Convert.ToInt32(checkCommand.ExecuteScalar());

    if (count > 0) return;

    // 50종 크리처 시드 데이터 (MVP)
    var creatures = new List<(string name, Rarity rarity, string desc)>
    {
        // Common (25종)
        ("슬라임", Rarity.Common, "말랑말랑한 젤리 생물"),
        ("꼬마구름", Rarity.Common, "둥실둥실 떠다니는 구름"),
        ("잎새", Rarity.Common, "바람에 흔들리는 잎사귀"),
        ("물방울", Rarity.Common, "투명하게 빛나는 물방울"),
        ("돌멩이", Rarity.Common, "단단한 작은 돌"),
        ("별똥별", Rarity.Common, "하늘에서 떨어진 작은 별"),
        ("꽃잎", Rarity.Common, "향기로운 분홍 꽃잎"),
        ("솜뭉치", Rarity.Common, "폭신폭신한 솜"),
        ("젤리콩", Rarity.Common, "달콤한 젤리 콩"),
        ("이끼돌", Rarity.Common, "이끼가 낀 귀여운 돌"),
        ("눈송이", Rarity.Common, "차가운 눈 결정"),
        ("반딧불", Rarity.Common, "밤에 빛나는 벌레"),
        ("씨앗", Rarity.Common, "가능성이 담긴 씨앗"),
        ("조약돌", Rarity.Common, "강에서 온 매끈한 돌"),
        ("먼지토끼", Rarity.Common, "뽀송뽀송한 먼지 덩어리"),
        ("비누방울", Rarity.Common, "무지개빛 비누방울"),
        ("도토리", Rarity.Common, "다람쥐가 좋아하는 열매"),
        ("꿀방울", Rarity.Common, "달콤한 황금 방울"),
        ("깃털", Rarity.Common, "가벼운 새 깃털"),
        ("이슬", Rarity.Common, "아침에 맺힌 이슬"),
        ("모래알", Rarity.Common, "해변의 작은 모래"),
        ("풀잎", Rarity.Common, "초록빛 풀잎"),
        ("나뭇가지", Rarity.Common, "작은 나무 조각"),
        ("진흙이", Rarity.Common, "말랑한 진흙 덩어리"),
        ("버섯", Rarity.Common, "동글동글한 버섯"),

        // Rare (15종)
        ("번개토끼", Rarity.Rare, "전기를 품은 토끼"),
        ("불꽃여우", Rarity.Rare, "꼬리에서 불꽃이 피는 여우"),
        ("얼음펭귄", Rarity.Rare, "차가운 기운의 펭귄"),
        ("바람새", Rarity.Rare, "바람을 타고 나는 새"),
        ("꽃사슴", Rarity.Rare, "뿔에 꽃이 피는 사슴"),
        ("달토끼", Rarity.Rare, "달빛을 받으면 빛나는 토끼"),
        ("무지개뱀", Rarity.Rare, "일곱 색깔 비늘의 뱀"),
        ("구름고래", Rarity.Rare, "하늘을 헤엄치는 고래"),
        ("수정나비", Rarity.Rare, "투명한 날개의 나비"),
        ("숲요정", Rarity.Rare, "숲을 지키는 작은 요정"),
        ("별똥곰", Rarity.Rare, "별빛 털을 가진 곰"),
        ("파도물개", Rarity.Rare, "파도를 타는 물개"),
        ("안개늑대", Rarity.Rare, "안개 속에서 나타나는 늑대"),
        ("노을새", Rarity.Rare, "저녁노을 빛깔의 새"),
        ("이끼거북", Rarity.Rare, "등에 정원이 있는 거북"),

        // Epic (7종)
        ("용아기", Rarity.Epic, "아직 어린 용"),
        ("유니콘", Rarity.Epic, "무지개 갈기의 유니콘"),
        ("피닉스", Rarity.Epic, "불꽃에서 다시 태어나는 새"),
        ("크라켄", Rarity.Epic, "심해의 거대 문어"),
        ("그리폰", Rarity.Epic, "독수리와 사자의 합체"),
        ("켈피", Rarity.Epic, "물속의 신비한 말"),
        ("바실리스크", Rarity.Epic, "눈빛이 무서운 뱀"),

        // Legendary (3종)
        ("황금드래곤", Rarity.Legendary, "전설의 황금빛 용"),
        ("세계수정령", Rarity.Legendary, "세계수를 지키는 정령"),
        ("시간고양이", Rarity.Legendary, "시간을 다루는 신비한 고양이"),
    };

    var insertCommand = connection.CreateCommand();
    insertCommand.CommandText = @"
        INSERT INTO creatures (name, rarity, sprite_path, description)
        VALUES (@name, @rarity, @sprite, @desc)
    ";

    for (int i = 0; i < creatures.Count; i++)
    {
        var (name, rarity, desc) = creatures[i];
        insertCommand.Parameters.Clear();
        insertCommand.Parameters.AddWithValue("@name", name);
        insertCommand.Parameters.AddWithValue("@rarity", (int)rarity);
        insertCommand.Parameters.AddWithValue("@sprite", $"Creatures/{i + 1}.png");
        insertCommand.Parameters.AddWithValue("@desc", desc);
        insertCommand.ExecuteNonQuery();
    }
}
```

**Step 2: 빌드 확인**

Run: `dotnet build`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add -A
git commit -m "feat: add creature seed data (50 creatures)"
```

---

## Phase 2: 핵심 서비스

### Task 8: EggService 생성

**Files:**
- Create: `TypingTamagotchi/Services/EggService.cs`

**Step 1: EggService 작성**

```csharp
using TypingTamagotchi.Models;

namespace TypingTamagotchi.Services;

public class EggService
{
    private readonly DatabaseService _db;
    private readonly Random _random = new();
    private Egg? _currentEgg;

    public event Action<Egg>? EggUpdated;
    public event Action<Egg>? EggReady;

    public EggService(DatabaseService db)
    {
        _db = db;
        LoadOrCreateEgg();
    }

    public Egg CurrentEgg => _currentEgg!;

    private void LoadOrCreateEgg()
    {
        using var connection = _db.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM current_egg WHERE id = 1";

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            _currentEgg = new Egg
            {
                Id = 1,
                Name = reader.GetString(1),
                SpritePath = reader.GetString(2),
                RequiredCount = reader.GetInt32(3),
                CurrentCount = reader.GetInt32(4)
            };
        }
        else
        {
            CreateNewEgg();
        }
    }

    public void CreateNewEgg()
    {
        var eggNames = new[] { "불꽃알", "물방울알", "바람알", "대지알", "번개알" };
        var name = eggNames[_random.Next(eggNames.Length)];
        var requiredCount = _random.Next(500, 2001); // 500~2000

        _currentEgg = new Egg
        {
            Id = 1,
            Name = name,
            SpritePath = $"Eggs/{name}.png",
            RequiredCount = requiredCount,
            CurrentCount = 0
        };

        SaveCurrentEgg();
        EggUpdated?.Invoke(_currentEgg);
    }

    public void AddProgress(int amount = 1)
    {
        if (_currentEgg == null) return;

        _currentEgg.CurrentCount += amount;
        SaveCurrentEgg();
        EggUpdated?.Invoke(_currentEgg);

        if (_currentEgg.IsReady)
        {
            EggReady?.Invoke(_currentEgg);
        }
    }

    private void SaveCurrentEgg()
    {
        if (_currentEgg == null) return;

        using var connection = _db.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO current_egg (id, name, sprite_path, required_count, current_count)
            VALUES (1, @name, @sprite, @required, @current)
        ";
        command.Parameters.AddWithValue("@name", _currentEgg.Name);
        command.Parameters.AddWithValue("@sprite", _currentEgg.SpritePath);
        command.Parameters.AddWithValue("@required", _currentEgg.RequiredCount);
        command.Parameters.AddWithValue("@current", _currentEgg.CurrentCount);
        command.ExecuteNonQuery();
    }
}
```

**Step 2: 빌드 확인**

Run: `dotnet build`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add -A
git commit -m "feat: add EggService for egg management"
```

---

### Task 9: HatchingService 생성

**Files:**
- Create: `TypingTamagotchi/Services/HatchingService.cs`

**Step 1: HatchingService 작성**

```csharp
using TypingTamagotchi.Models;

namespace TypingTamagotchi.Services;

public class HatchingService
{
    private readonly DatabaseService _db;
    private readonly Random _random = new();

    public event Action<Creature>? CreatureHatched;

    public HatchingService(DatabaseService db)
    {
        _db = db;
    }

    public Creature Hatch()
    {
        var rarity = RollRarity();
        var creature = GetRandomCreatureByRarity(rarity);
        SaveToCollection(creature);
        CreatureHatched?.Invoke(creature);
        return creature;
    }

    private Rarity RollRarity()
    {
        var roll = _random.Next(100);

        // Common: 50%, Rare: 30%, Epic: 15%, Legendary: 5%
        return roll switch
        {
            < 50 => Rarity.Common,
            < 80 => Rarity.Rare,
            < 95 => Rarity.Epic,
            _ => Rarity.Legendary
        };
    }

    private Creature GetRandomCreatureByRarity(Rarity rarity)
    {
        using var connection = _db.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, name, rarity, sprite_path, description
            FROM creatures
            WHERE rarity = @rarity
            ORDER BY RANDOM()
            LIMIT 1
        ";
        command.Parameters.AddWithValue("@rarity", (int)rarity);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new Creature
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Rarity = (Rarity)reader.GetInt32(2),
                SpritePath = reader.GetString(3),
                Description = reader.GetString(4)
            };
        }

        throw new InvalidOperationException($"No creature found for rarity {rarity}");
    }

    private void SaveToCollection(Creature creature)
    {
        using var connection = _db.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO collection (creature_id, obtained_at)
            VALUES (@creatureId, @obtainedAt)
        ";
        command.Parameters.AddWithValue("@creatureId", creature.Id);
        command.Parameters.AddWithValue("@obtainedAt", DateTime.Now.ToString("o"));
        command.ExecuteNonQuery();
    }

    public List<(Creature creature, int count, DateTime firstObtained)> GetCollection()
    {
        using var connection = _db.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT c.id, c.name, c.rarity, c.sprite_path, c.description,
                   COUNT(*) as count, MIN(col.obtained_at) as first_obtained
            FROM collection col
            JOIN creatures c ON col.creature_id = c.id
            GROUP BY c.id
            ORDER BY c.rarity DESC, c.name
        ";

        var results = new List<(Creature, int, DateTime)>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var creature = new Creature
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Rarity = (Rarity)reader.GetInt32(2),
                SpritePath = reader.GetString(3),
                Description = reader.GetString(4)
            };
            var count = reader.GetInt32(5);
            var firstObtained = DateTime.Parse(reader.GetString(6));
            results.Add((creature, count, firstObtained));
        }

        return results;
    }

    public int GetTotalCreatureCount() => 50;

    public int GetOwnedCreatureCount()
    {
        using var connection = _db.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(DISTINCT creature_id) FROM collection";
        return Convert.ToInt32(command.ExecuteScalar());
    }
}
```

**Step 2: 빌드 확인**

Run: `dotnet build`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add -A
git commit -m "feat: add HatchingService for creature hatching"
```

---

### Task 10: InputService 생성 (인터페이스 + 시뮬레이터)

**Files:**
- Create: `TypingTamagotchi/Services/IInputService.cs`
- Create: `TypingTamagotchi/Services/SimulatedInputService.cs`

**Step 1: IInputService 인터페이스 작성**

```csharp
namespace TypingTamagotchi.Services;

public interface IInputService
{
    event Action? InputDetected;
    void Start();
    void Stop();
}
```

**Step 2: SimulatedInputService 작성 (개발/테스트용)**

```csharp
using System.Timers;
using Timer = System.Timers.Timer;

namespace TypingTamagotchi.Services;

public class SimulatedInputService : IInputService
{
    private Timer? _timer;

    public event Action? InputDetected;

    public void Start()
    {
        _timer = new Timer(100); // 0.1초마다 입력 시뮬레이션
        _timer.Elapsed += OnTimerElapsed;
        _timer.Start();
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        InputDetected?.Invoke();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }
}
```

**Step 3: 빌드 확인**

Run: `dotnet build`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add -A
git commit -m "feat: add IInputService interface and simulated implementation"
```

---

## Phase 3: UI 구현

### Task 11: MainWindowViewModel 수정

**Files:**
- Modify: `TypingTamagotchi/ViewModels/MainWindowViewModel.cs`

**Step 1: ViewModel 전체 재작성**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TypingTamagotchi.Models;
using TypingTamagotchi.Services;

namespace TypingTamagotchi.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly DatabaseService _db;
    private readonly EggService _eggService;
    private readonly HatchingService _hatchingService;
    private readonly IInputService _inputService;

    [ObservableProperty]
    private string _eggName = "";

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _progressText = "0%";

    [ObservableProperty]
    private bool _isHatchPopupVisible;

    [ObservableProperty]
    private Creature? _hatchedCreature;

    [ObservableProperty]
    private string _collectionStatus = "0/50";

    public MainWindowViewModel()
    {
        _db = new DatabaseService();
        _db.SeedCreaturesIfEmpty();

        _eggService = new EggService(_db);
        _hatchingService = new HatchingService(_db);
        _inputService = new SimulatedInputService();

        _eggService.EggUpdated += OnEggUpdated;
        _eggService.EggReady += OnEggReady;
        _inputService.InputDetected += OnInputDetected;

        UpdateEggDisplay();
        UpdateCollectionStatus();

        _inputService.Start();
    }

    private void OnInputDetected()
    {
        _eggService.AddProgress(1);
    }

    private void OnEggUpdated(Egg egg)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            UpdateEggDisplay();
        });
    }

    private void OnEggReady(Egg egg)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _inputService.Stop();
            var creature = _hatchingService.Hatch();
            HatchedCreature = creature;
            IsHatchPopupVisible = true;
            UpdateCollectionStatus();
        });
    }

    private void UpdateEggDisplay()
    {
        var egg = _eggService.CurrentEgg;
        EggName = egg.Name;
        Progress = egg.Progress;
        ProgressText = $"{(int)(egg.Progress * 100)}%";
    }

    private void UpdateCollectionStatus()
    {
        var owned = _hatchingService.GetOwnedCreatureCount();
        var total = _hatchingService.GetTotalCreatureCount();
        CollectionStatus = $"{owned}/{total}";
    }

    [RelayCommand]
    private void CloseHatchPopup()
    {
        IsHatchPopupVisible = false;
        _eggService.CreateNewEgg();
        _inputService.Start();
    }
}
```

**Step 2: 빌드 확인**

Run: `dotnet build`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add -A
git commit -m "feat: implement MainWindowViewModel with game logic"
```

---

### Task 12: MainWindow UI 구현

**Files:**
- Modify: `TypingTamagotchi/Views/MainWindow.axaml`

**Step 1: MainWindow XAML 재작성**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:TypingTamagotchi.ViewModels"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d" d:DesignWidth="400" d:DesignHeight="300"
        x:Class="TypingTamagotchi.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Title="Typing Tamagotchi"
        Width="400" Height="300"
        WindowStartupLocation="CenterScreen">

    <Design.DataContext>
        <vm:MainWindowViewModel/>
    </Design.DataContext>

    <Grid>
        <!-- 메인 화면 -->
        <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center" Spacing="20">
            <!-- 알 이름 -->
            <TextBlock Text="{Binding EggName}"
                       FontSize="24"
                       FontWeight="Bold"
                       HorizontalAlignment="Center"/>

            <!-- 알 이미지 (placeholder) -->
            <Border Width="100" Height="100"
                    Background="#FFE4B5"
                    CornerRadius="50"
                    HorizontalAlignment="Center">
                <TextBlock Text="🥚"
                           FontSize="48"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center"/>
            </Border>

            <!-- 진행도 -->
            <StackPanel Spacing="5">
                <ProgressBar Value="{Binding Progress}"
                             Minimum="0" Maximum="1"
                             Width="250" Height="20"/>
                <TextBlock Text="{Binding ProgressText}"
                           HorizontalAlignment="Center"/>
            </StackPanel>

            <!-- 수집 현황 -->
            <TextBlock Text="{Binding CollectionStatus, StringFormat='수집: {0}'}"
                       HorizontalAlignment="Center"
                       Foreground="Gray"/>
        </StackPanel>

        <!-- 부화 팝업 -->
        <Border IsVisible="{Binding IsHatchPopupVisible}"
                Background="#80000000"
                HorizontalAlignment="Stretch"
                VerticalAlignment="Stretch">
            <Border Background="White"
                    CornerRadius="10"
                    Padding="30"
                    HorizontalAlignment="Center"
                    VerticalAlignment="Center"
                    MinWidth="300">
                <StackPanel Spacing="15">
                    <TextBlock Text="✨ 부화! ✨"
                               FontSize="24"
                               FontWeight="Bold"
                               HorizontalAlignment="Center"/>

                    <!-- 크리처 이미지 placeholder -->
                    <Border Width="80" Height="80"
                            Background="#E8F5E9"
                            CornerRadius="40"
                            HorizontalAlignment="Center">
                        <TextBlock Text="🐣"
                                   FontSize="36"
                                   HorizontalAlignment="Center"
                                   VerticalAlignment="Center"/>
                    </Border>

                    <TextBlock Text="{Binding HatchedCreature.Name}"
                               FontSize="20"
                               FontWeight="SemiBold"
                               HorizontalAlignment="Center"/>

                    <TextBlock Text="{Binding HatchedCreature.Rarity}"
                               HorizontalAlignment="Center"
                               Foreground="Purple"/>

                    <TextBlock Text="{Binding HatchedCreature.Description}"
                               HorizontalAlignment="Center"
                               Foreground="Gray"
                               TextWrapping="Wrap"/>

                    <Button Content="확인"
                            Command="{Binding CloseHatchPopupCommand}"
                            HorizontalAlignment="Center"
                            Padding="30,10"/>
                </StackPanel>
            </Border>
        </Border>
    </Grid>
</Window>
```

**Step 2: 빌드 확인**

Run: `dotnet build`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add -A
git commit -m "feat: implement MainWindow UI with egg display and hatch popup"
```

---

### Task 13: 실행 테스트

**Step 1: 앱 실행**

Run: `cd ~/typing-tamagotchi/TypingTamagotchi && dotnet run`

Expected:
- 창이 열림
- 알 이름과 게이지가 표시됨
- 게이지가 자동으로 채워짐 (시뮬레이션)
- 100% 도달 시 부화 팝업 표시
- 확인 버튼 누르면 새 알 시작

**Step 2: 문제 있으면 수정**

**Step 3: Commit (문제 수정 시)**

```bash
git add -A
git commit -m "fix: resolve issues from initial testing"
```

---

## Phase 4: 추가 기능 (선택)

### Task 14: 도감 화면 (CollectionView)

> 이 태스크는 Phase 3 완료 후 진행

**Files:**
- Create: `TypingTamagotchi/Views/CollectionWindow.axaml`
- Create: `TypingTamagotchi/Views/CollectionWindow.axaml.cs`
- Create: `TypingTamagotchi/ViewModels/CollectionViewModel.cs`

(상세 구현은 Phase 3 완료 후 진행)

---

### Task 15: 시스템 트레이 (추후)

> Windows 전용 기능, Windows에서 개발 시 진행

---

### Task 16: 전역 입력 후킹 (추후)

> Windows: SetWindowsHookEx 사용
> Linux: libinput 또는 X11 후킹
> 플랫폼별 구현 필요

---

## 실행 순서 요약

1. **Phase 1** (Task 1-7): 데이터 모델 및 DB 설정
2. **Phase 2** (Task 8-10): 핵심 서비스 구현
3. **Phase 3** (Task 11-13): UI 구현 및 테스트
4. **Phase 4** (Task 14-16): 추가 기능 (선택)

예상 커밋 수: 약 12개
