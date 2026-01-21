using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TypingTamagotchi.Models;
using TypingTamagotchi.Services;

namespace TypingTamagotchi.ViewModels;

public partial class CollectionViewModel : ViewModelBase
{
    private readonly DatabaseService _db = null!;

    // 진열장 변경 이벤트 (MiniWidget에서 구독)
    public static event Action? DisplayChanged;

    // 놀이터 변경 이벤트
    public static event Action? PlaygroundChanged;

    [ObservableProperty]
    private ObservableCollection<CollectionItem> _items = new();

    [ObservableProperty]
    private string _collectionStatus = "";

    [ObservableProperty]
    private CollectionItem? _selectedItem;

    public CollectionViewModel()
    {
        try
        {
            _db = new DatabaseService();
            LoadCollection();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CollectionViewModel 초기화 에러: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            CollectionStatus = "로딩 실패";
        }
    }

    private void LoadCollection()
    {
        var hatching = new HatchingService(_db);

        // 모든 크리처 가져오기
        var allCreatures = GetAllCreatures(_db);

        // 획득한 크리처 정보
        var collection = hatching.GetCollection();
        var ownedIds = collection.Select(c => c.creature.Id).ToHashSet();

        // 현재 진열장 상태
        var displaySlots = _db.GetDisplaySlots();
        var displayedIds = displaySlots.Select(s => s.creatureId).ToHashSet();

        // 현재 놀이터 상태
        var playgroundCreatures = _db.GetPlaygroundCreatures();
        var playgroundIds = playgroundCreatures.Select(p => p.creatureId).ToHashSet();

        Items.Clear();
        foreach (var creature in allCreatures)
        {
            var owned = collection.FirstOrDefault(c => c.creature.Id == creature.Id);
            Items.Add(new CollectionItem
            {
                Creature = creature,
                IsOwned = ownedIds.Contains(creature.Id),
                Count = owned.count,
                FirstObtained = owned.firstObtained,
                IsInDisplay = displayedIds.Contains(creature.Id),
                IsInPlayground = playgroundIds.Contains(creature.Id)
            });
        }

        var ownedCount = hatching.GetOwnedCreatureCount();
        var totalCount = hatching.GetTotalCreatureCount();
        CollectionStatus = $"수집: {ownedCount}/{totalCount} ({ownedCount * 100 / totalCount}%)";
    }

    [RelayCommand]
    private void ToggleDisplay(CollectionItem? item)
    {
        if (item == null || !item.IsOwned)
            return;

        // 놀이터에 있으면 진열장에 추가 불가
        if (item.IsInPlayground && !item.IsInDisplay)
            return;

        if (item.IsInDisplay)
        {
            // 진열장에서 제거
            _db.RemoveCreatureFromDisplay(item.Creature.Id);
            item.IsInDisplay = false;
        }
        else
        {
            // 빈 슬롯 찾기
            var nextSlot = _db.GetNextAvailableSlot();
            if (nextSlot >= 0)
            {
                _db.SetDisplaySlot(nextSlot, item.Creature.Id);
                item.IsInDisplay = true;
            }
        }

        // 진열장 실시간 업데이트
        DisplayChanged?.Invoke();
    }

    [RelayCommand]
    private void TogglePlayground(CollectionItem? item)
    {
        if (item == null || !item.IsOwned)
            return;

        // 진열장에 있으면 놀이터에 추가 불가
        if (item.IsInDisplay && !item.IsInPlayground)
            return;

        if (item.IsInPlayground)
        {
            // 놀이터에서 제거
            _db.RemoveFromPlayground(item.Creature.Id);
            item.IsInPlayground = false;
        }
        else
        {
            // 놀이터 슬롯 확인 (최대 4마리)
            if (_db.GetPlaygroundCount() < 4)
            {
                _db.AddToPlayground(item.Creature.Id);
                item.IsInPlayground = true;
            }
        }

        // 놀이터 실시간 업데이트
        PlaygroundChanged?.Invoke();
    }

    private static System.Collections.Generic.List<Creature> GetAllCreatures(DatabaseService db)
    {
        using var connection = db.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, name, rarity, element, sprite_path, description,
                   age, gender, favorite_food, dislikes, background
            FROM creatures
            ORDER BY rarity DESC, name
        ";

        var creatures = new System.Collections.Generic.List<Creature>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            creatures.Add(new Creature
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Rarity = (Rarity)reader.GetInt32(2),
                Element = (Element)reader.GetInt32(3),
                SpritePath = reader.GetString(4),
                Description = reader.GetString(5),
                Age = reader.IsDBNull(6) ? "" : reader.GetString(6),
                Gender = reader.IsDBNull(7) ? "" : reader.GetString(7),
                FavoriteFood = reader.IsDBNull(8) ? "" : reader.GetString(8),
                Dislikes = reader.IsDBNull(9) ? "" : reader.GetString(9),
                Background = reader.IsDBNull(10) ? "" : reader.GetString(10)
            });
        }

        return creatures;
    }
}

public partial class CollectionItem : ObservableObject
{
    public Creature Creature { get; set; } = null!;
    public bool IsOwned { get; set; }
    public int Count { get; set; }
    public DateTime FirstObtained { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayButtonText))]
    [NotifyPropertyChangedFor(nameof(CanSendToPlayground))]
    private bool _isInDisplay;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlaygroundButtonText))]
    [NotifyPropertyChangedFor(nameof(CanSendToDisplay))]
    private bool _isInPlayground;

    public string DisplayName => IsOwned ? Creature.Name : "???";
    public string RarityText => IsOwned ? Creature.Rarity.ToString() : "";
    public string CountText => IsOwned && Count > 1 ? $"x{Count}" : "";
    public double Opacity => IsOwned ? 1.0 : 0.3;
    public string DisplayButtonText => IsInDisplay ? "진열장에서 제거" : "진열장에 추가";
    public string PlaygroundButtonText => IsInPlayground ? "놀이터에서 제거" : "놀러 보내기";
    public bool CanSendToDisplay => !IsInPlayground;
    public bool CanSendToPlayground => !IsInDisplay;
}
