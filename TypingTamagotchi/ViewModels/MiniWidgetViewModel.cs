using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using TypingTamagotchi.Models;
using TypingTamagotchi.Services;

namespace TypingTamagotchi.ViewModels;

public partial class MiniWidgetViewModel : ViewModelBase
{
    private readonly DatabaseService _db;
    private readonly HatchingService _hatching;
    private readonly Timer _clockTimer;
    private readonly Random _random = new();
    private const double PROGRESS_BAR_MAX_WIDTH = 180.0;

    // 알 종류 (이름, 이미지 파일명, 이모지, 레전더리 여부)
    private static readonly (string name, string image, string emoji, bool isLegendary)[] EggTypes = new[]
    {
        ("불꽃알", "불꽃알.png", "🔥", false),
        ("물방울알", "물방울알.png", "💧", false),
        ("바람알", "바람알.png", "🌿", false),
        ("대지알", "대지알.png", "🪨", false),
        ("번개알", "번개알.png", "⚡", false),
        ("전설알", "전설알.png", "👑", true)  // 5% 확률, 레전더리 확정
    };

    private int _currentEggTypeIndex = 0;
    private Element _currentEggElement = Element.Fire;
    private bool _isLegendaryEgg = false;

    [ObservableProperty]
    private string _eggName = "불꽃알";

    [ObservableProperty]
    private Bitmap? _eggImage;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _progressText = "0%";

    [ObservableProperty]
    private double _progressWidth = 0;

    [ObservableProperty]
    private string _displayCountText = "";

    [ObservableProperty]
    private ObservableCollection<DisplaySlot> _displaySlots = new();

    // 시계
    [ObservableProperty]
    private string _currentTime = "";

    [ObservableProperty]
    private string _currentDate = "";

    // 수집 현황
    [ObservableProperty]
    private string _collectionText = "0/50";

    [ObservableProperty]
    private double _collectionProgress = 0;

    [ObservableProperty]
    private double _collectionProgressWidth = 0;

    // 진열장 토글
    [ObservableProperty]
    private bool _isShowcaseVisible = true;

    // 토글 버튼 텍스트
    public string ToggleButtonText => IsShowcaseVisible ? "▼ 접기" : "▶ 펼치기";

    // 미니모드 (접힌 상태)
    public bool IsMiniMode => !IsShowcaseVisible;

    partial void OnIsShowcaseVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(ToggleButtonText));
        OnPropertyChanged(nameof(IsMiniMode));
    }

    private const double COLLECTION_BAR_MAX_WIDTH = 60.0;

    public MiniWidgetViewModel()
    {
        _db = new DatabaseService();
        _hatching = new HatchingService(_db);

        // 12개 슬롯 초기화 (3x4 그리드)
        for (int i = 0; i < 12; i++)
        {
            DisplaySlots.Add(new DisplaySlot { SlotIndex = i });
        }

        // 시계 초기화
        UpdateClock();
        _clockTimer = new Timer(1000);
        _clockTimer.Elapsed += (s, e) => Dispatcher.UIThread.Post(UpdateClock);
        _clockTimer.Start();

        LoadDisplayCreatures();
        UpdateProgress();
        RandomizeEgg(); // 초기 알 종류 설정
    }

    // 슬롯머신용 알 정보 접근
    public static int EggTypeCount => EggTypes.Length;
    public static (string name, string image, string emoji, bool isLegendary) GetEggType(int index) => EggTypes[index];

    // 슬롯머신에서 선택된 알 설정
    public void SetEggByIndex(int index)
    {
        _currentEggTypeIndex = index;
        _isLegendaryEgg = EggTypes[index].isLegendary;

        var egg = EggTypes[index];
        EggName = egg.name;

        // 알 종류에 따른 속성 설정 (레전더리는 랜덤)
        if (_isLegendaryEgg)
        {
            _currentEggElement = (Element)_random.Next(5);
        }
        else
        {
            _currentEggElement = index switch
            {
                0 => Element.Fire,      // 불꽃알
                1 => Element.Water,     // 물방울알
                2 => Element.Wind,      // 바람알
                3 => Element.Earth,     // 대지알
                4 => Element.Lightning, // 번개알
                _ => Element.Fire
            };
        }

        // 알 이미지 로드
        try
        {
            var uri = new Uri($"avares://TypingTamagotchi/Assets/Eggs/{egg.image}");
            EggImage = new Bitmap(Avalonia.Platform.AssetLoader.Open(uri));
        }
        catch
        {
            EggImage = null;
        }
    }

    private void RandomizeEgg()
    {
        // 5% 확률로 레전더리 알
        int index;
        if (_random.Next(100) < 5)
        {
            index = 5; // 전설알
        }
        else
        {
            index = _random.Next(5); // 일반 알 (0-4)
        }
        SetEggByIndex(index);
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        CurrentTime = now.ToString("HH:mm:ss");
        CurrentDate = now.ToString("MM/dd ddd");
    }

    public void LoadDisplayCreatures()
    {
        // 저장된 진열장 슬롯 로드
        var savedSlots = _db.GetDisplaySlots();
        var collection = _hatching.GetCollection();
        var creatureMap = collection.ToDictionary(c => c.creature.Id, c => c.creature);

        Console.WriteLine($"[MiniWidget] Saved slots: {savedSlots.Count}, Collection: {collection.Count}");

        // 모든 슬롯 초기화
        for (int i = 0; i < 12; i++)
        {
            DisplaySlots[i].Clear();
        }

        // 저장된 슬롯에 크리처 배치
        foreach (var (slotIndex, creatureId) in savedSlots)
        {
            Console.WriteLine($"[MiniWidget] Placing creature ID {creatureId} in slot {slotIndex}");
            if (slotIndex >= 0 && slotIndex < 12 && creatureMap.TryGetValue(creatureId, out var creature))
            {
                Console.WriteLine($"[MiniWidget] -> Success: {creature.Name}");
                DisplaySlots[slotIndex].SetCreature(creature);
            }
            else
            {
                Console.WriteLine($"[MiniWidget] -> Failed: creature not in map");
            }
        }

        var totalOwned = _hatching.GetOwnedCreatureCount();
        var totalCreatures = _hatching.GetTotalCreatureCount();
        DisplayCountText = $"({totalOwned}/{totalCreatures})";

        // 수집 현황 업데이트
        CollectionText = $"{totalOwned}/{totalCreatures}";
        CollectionProgress = (double)totalOwned / totalCreatures;
        CollectionProgressWidth = CollectionProgress * COLLECTION_BAR_MAX_WIDTH;
    }

    // 부화 이벤트 (UI에서 토스트 표시용)
    public event Action<Creature>? CreatureHatched;

    public void UpdateProgress()
    {
        var (keystrokes, clicks) = _hatching.GetCurrentProgress();
        var totalInputs = keystrokes + clicks;
        var required = 1500; // 부화에 필요한 입력

        Progress = Math.Min(1.0, (double)totalInputs / required);
        ProgressText = $"{(int)(Progress * 100)}%";
        ProgressWidth = Progress * PROGRESS_BAR_MAX_WIDTH;
    }

    public void OnInput()
    {
        _hatching.RecordInput(isClick: false);
        UpdateProgress();

        // 부화 체크
        if (Progress >= 1.0)
        {
            Creature? creature;

            if (_isLegendaryEgg)
            {
                // 레전더리 알이면 레전더리 크리처 확정
                creature = _hatching.TryHatchLegendary();
            }
            else
            {
                // 일반 알이면 속성별 부화
                creature = _hatching.TryHatchByElement(_currentEggElement);
            }

            if (creature != null)
            {
                LoadDisplayCreatures();
                UpdateProgress();

                // 부화 이벤트 발생 (토스트 표시용)
                // 슬롯머신은 부화 팝업이 닫힌 후 View에서 호출됨
                CreatureHatched?.Invoke(creature);
            }
        }
    }

    public void SwapSlots(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= 12 || toIndex < 0 || toIndex >= 12)
            return;
        if (fromIndex == toIndex)
            return;

        // 슬롯 내용 교환
        var fromSlot = DisplaySlots[fromIndex];
        var toSlot = DisplaySlots[toIndex];

        // 임시 저장
        var tempCreature = fromSlot.GetCreature();
        var tempHasCreature = fromSlot.HasCreature;
        var toCreature = toSlot.GetCreature();
        var toHasCreature = toSlot.HasCreature;

        // UI 교환
        if (toHasCreature)
        {
            fromSlot.SetCreature(toCreature!);
        }
        else
        {
            fromSlot.Clear();
        }

        if (tempHasCreature && tempCreature != null)
        {
            toSlot.SetCreature(tempCreature);
        }
        else
        {
            toSlot.Clear();
        }

        // DB 저장
        if (tempHasCreature && tempCreature != null)
        {
            _db.SetDisplaySlot(toIndex, tempCreature.Id);
        }
        else
        {
            _db.RemoveFromDisplaySlot(toIndex);
        }

        if (toHasCreature && toCreature != null)
        {
            _db.SetDisplaySlot(fromIndex, toCreature.Id);
        }
        else
        {
            _db.RemoveFromDisplaySlot(fromIndex);
        }
    }

    public void Refresh()
    {
        LoadDisplayCreatures();
        UpdateProgress();
    }
}

public partial class DisplaySlot : ObservableObject
{
    private Creature? _creature;

    [ObservableProperty]
    private int _slotIndex;

    [ObservableProperty]
    private bool _isEmpty = true;

    [ObservableProperty]
    private bool _hasCreature = false;

    [ObservableProperty]
    private string _creatureName = "빈 슬롯";

    [ObservableProperty]
    private string _rarityText = "";

    [ObservableProperty]
    private Bitmap? _creatureImage;

    [ObservableProperty]
    private Bitmap? _pedestalImage;

    [ObservableProperty]
    private IBrush _slotBackground = new SolidColorBrush(Color.Parse("#20FFFFFF"));

    [ObservableProperty]
    private IBrush _rarityColor = new SolidColorBrush(Color.Parse("#666666"));

    [ObservableProperty]
    private bool _isDragOver = false;

    [ObservableProperty]
    private IBrush? _auraBrush = null;

    [ObservableProperty]
    private bool _hasAura = false;

    public Creature? GetCreature() => _creature;

    public void SetCreature(Creature creature)
    {
        _creature = creature;
        IsEmpty = false;
        HasCreature = true;
        CreatureName = creature.Name;

        // 등급 텍스트
        RarityText = creature.Rarity switch
        {
            Rarity.Legendary => "★★★★ 전설",
            Rarity.Epic => "★★★ 영웅",
            Rarity.Rare => "★★ 희귀",
            _ => "★ 일반"
        };

        // 등급별 보석 색상 (더 화려하게)
        RarityColor = creature.Rarity switch
        {
            Rarity.Legendary => new LinearGradientBrush
            {
                StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                EndPoint = new Avalonia.RelativePoint(1, 1, Avalonia.RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.Parse("#FFD700"), 0),
                    new GradientStop(Color.Parse("#FFF8DC"), 0.5),
                    new GradientStop(Color.Parse("#FFD700"), 1)
                }
            },
            Rarity.Epic => new LinearGradientBrush
            {
                StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                EndPoint = new Avalonia.RelativePoint(1, 1, Avalonia.RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.Parse("#9C27B0"), 0),
                    new GradientStop(Color.Parse("#E040FB"), 0.5),
                    new GradientStop(Color.Parse("#9C27B0"), 1)
                }
            },
            Rarity.Rare => new LinearGradientBrush
            {
                StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                EndPoint = new Avalonia.RelativePoint(1, 1, Avalonia.RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.Parse("#2196F3"), 0),
                    new GradientStop(Color.Parse("#64B5F6"), 0.5),
                    new GradientStop(Color.Parse("#2196F3"), 1)
                }
            },
            _ => new LinearGradientBrush
            {
                StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                EndPoint = new Avalonia.RelativePoint(1, 1, Avalonia.RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.Parse("#4CAF50"), 0),
                    new GradientStop(Color.Parse("#81C784"), 0.5),
                    new GradientStop(Color.Parse("#4CAF50"), 1)
                }
            }
        };

        // 등급별 슬롯 배경 (어두운 톤)
        SlotBackground = creature.Rarity switch
        {
            Rarity.Legendary => new LinearGradientBrush
            {
                StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                EndPoint = new Avalonia.RelativePoint(0, 1, Avalonia.RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.Parse("#3D3520"), 0),
                    new GradientStop(Color.Parse("#2A2515"), 1)
                }
            },
            Rarity.Epic => new LinearGradientBrush
            {
                StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                EndPoint = new Avalonia.RelativePoint(0, 1, Avalonia.RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.Parse("#2D1F3D"), 0),
                    new GradientStop(Color.Parse("#1A1225"), 1)
                }
            },
            Rarity.Rare => new LinearGradientBrush
            {
                StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                EndPoint = new Avalonia.RelativePoint(0, 1, Avalonia.RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.Parse("#1A2A3D"), 0),
                    new GradientStop(Color.Parse("#0F1A25"), 1)
                }
            },
            _ => new LinearGradientBrush
            {
                StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                EndPoint = new Avalonia.RelativePoint(0, 1, Avalonia.RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.Parse("#1F2D1F"), 0),
                    new GradientStop(Color.Parse("#151F15"), 1)
                }
            }
        };

        // 이미지 로드
        LoadImage(creature.SpritePath);

        // 받침대 이미지 로드 (희귀도별)
        var pedestalName = creature.Rarity switch
        {
            Rarity.Legendary => "pedestal_legendary.png",
            Rarity.Epic => "pedestal_epic.png",
            Rarity.Rare => "pedestal_rare.png",
            _ => "pedestal_common.png"
        };
        LoadPedestalImage(pedestalName);

        // 등급별 아우라 효과
        HasAura = creature.Rarity != Rarity.Common;
        AuraBrush = creature.Rarity switch
        {
            Rarity.Legendary => new RadialGradientBrush
            {
                Center = new Avalonia.RelativePoint(0.5, 0.5, Avalonia.RelativeUnit.Relative),
                GradientOrigin = new Avalonia.RelativePoint(0.5, 0.5, Avalonia.RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.Parse("#80FFD700"), 0),
                    new GradientStop(Color.Parse("#40FFD700"), 0.5),
                    new GradientStop(Color.Parse("#00FFD700"), 1)
                }
            },
            Rarity.Epic => new RadialGradientBrush
            {
                Center = new Avalonia.RelativePoint(0.5, 0.5, Avalonia.RelativeUnit.Relative),
                GradientOrigin = new Avalonia.RelativePoint(0.5, 0.5, Avalonia.RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.Parse("#609C27B0"), 0),
                    new GradientStop(Color.Parse("#309C27B0"), 0.5),
                    new GradientStop(Color.Parse("#009C27B0"), 1)
                }
            },
            Rarity.Rare => new RadialGradientBrush
            {
                Center = new Avalonia.RelativePoint(0.5, 0.5, Avalonia.RelativeUnit.Relative),
                GradientOrigin = new Avalonia.RelativePoint(0.5, 0.5, Avalonia.RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.Parse("#402196F3"), 0),
                    new GradientStop(Color.Parse("#202196F3"), 0.5),
                    new GradientStop(Color.Parse("#002196F3"), 1)
                }
            },
            _ => null
        };
    }

    private void LoadPedestalImage(string pedestalName)
    {
        try
        {
            var basePath = AppContext.BaseDirectory;
            var filePath = System.IO.Path.Combine(basePath, "Assets", "UI", pedestalName);

            if (System.IO.File.Exists(filePath))
            {
                PedestalImage = new Bitmap(filePath);
            }
            else
            {
                var uri = new Uri($"avares://TypingTamagotchi/Assets/UI/{pedestalName}");
                PedestalImage = new Bitmap(Avalonia.Platform.AssetLoader.Open(uri));
            }
        }
        catch
        {
            PedestalImage = null;
        }
    }

    private void LoadImage(string spritePath)
    {
        try
        {
            var basePath = AppContext.BaseDirectory;
            var filePath = System.IO.Path.Combine(basePath, "Assets", spritePath);

            if (System.IO.File.Exists(filePath))
            {
                CreatureImage = new Bitmap(filePath);
            }
            else
            {
                // avares 시도
                var uri = new Uri($"avares://TypingTamagotchi/Assets/{spritePath}");
                CreatureImage = new Bitmap(Avalonia.Platform.AssetLoader.Open(uri));
            }
        }
        catch
        {
            CreatureImage = null;
        }
    }

    public void Clear()
    {
        _creature = null;
        IsEmpty = true;
        HasCreature = false;
        CreatureName = "빈 슬롯";
        RarityText = "";
        CreatureImage = null;
        PedestalImage = null;
        SlotBackground = new SolidColorBrush(Color.Parse("#20FFFFFF"));
        RarityColor = new SolidColorBrush(Color.Parse("#666666"));
        AuraBrush = null;
        HasAura = false;
    }
}
