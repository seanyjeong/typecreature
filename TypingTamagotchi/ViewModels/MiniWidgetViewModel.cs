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
    private const double PROGRESS_BAR_MAX_WIDTH = 180.0;

    [ObservableProperty]
    private string _eggName = "신비한 알";

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

    public MiniWidgetViewModel()
    {
        _db = new DatabaseService();
        _hatching = new HatchingService(_db);

        // 10개 슬롯 초기화
        for (int i = 0; i < 10; i++)
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

        Console.WriteLine($"[MiniWidget] 저장된 슬롯: {savedSlots.Count}, 컬렉션: {collection.Count}");

        // 모든 슬롯 초기화
        for (int i = 0; i < 10; i++)
        {
            DisplaySlots[i].Clear();
        }

        // 저장된 슬롯에 크리처 배치
        foreach (var (slotIndex, creatureId) in savedSlots)
        {
            Console.WriteLine($"[MiniWidget] 슬롯 {slotIndex}에 크리처 ID {creatureId} 배치 시도");
            if (slotIndex >= 0 && slotIndex < 10 && creatureMap.TryGetValue(creatureId, out var creature))
            {
                Console.WriteLine($"[MiniWidget] -> 성공: {creature.Name}");
                DisplaySlots[slotIndex].SetCreature(creature);
            }
            else
            {
                Console.WriteLine($"[MiniWidget] -> 실패: 크리처 맵에 없음");
            }
        }

        var totalOwned = _hatching.GetOwnedCreatureCount();
        var totalCreatures = _hatching.GetTotalCreatureCount();
        DisplayCountText = $"({totalOwned}/{totalCreatures})";

        // 수집 현황 업데이트
        CollectionText = $"{totalOwned}/{totalCreatures}";
        CollectionProgress = (double)totalOwned / totalCreatures;
    }

    // 부화 이벤트 (UI에서 토스트 표시용)
    public event Action<Creature>? CreatureHatched;

    public void UpdateProgress()
    {
        var (keystrokes, clicks) = _hatching.GetCurrentProgress();
        var totalInputs = keystrokes + clicks;
        var required = 1000; // 부화에 필요한 입력 (2배 느리게)

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
            var creature = _hatching.TryHatch();
            if (creature != null)
            {
                LoadDisplayCreatures();
                UpdateProgress();

                // 부화 이벤트 발생 (토스트 표시용)
                CreatureHatched?.Invoke(creature);
            }
        }
    }

    public void SwapSlots(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= 10 || toIndex < 0 || toIndex >= 10)
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
    private IBrush _slotBackground = new SolidColorBrush(Color.Parse("#20FFFFFF"));

    [ObservableProperty]
    private IBrush _rarityColor = new SolidColorBrush(Color.Parse("#666666"));

    [ObservableProperty]
    private bool _isDragOver = false;

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
        SlotBackground = new SolidColorBrush(Color.Parse("#20FFFFFF"));
        RarityColor = new SolidColorBrush(Color.Parse("#666666"));
    }
}
