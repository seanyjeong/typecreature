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
    private readonly DatabaseService _db;

    [ObservableProperty]
    private ObservableCollection<CollectionItem> _items = new();

    [ObservableProperty]
    private string _collectionStatus = "";

    [ObservableProperty]
    private CollectionItem? _selectedItem;

    public CollectionViewModel()
    {
        _db = new DatabaseService();
        LoadCollection();
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
                IsInDisplay = displayedIds.Contains(creature.Id)
            });
        }

        var ownedCount = hatching.GetOwnedCreatureCount();
        var totalCount = hatching.GetTotalCreatureCount();
        CollectionStatus = $"수집: {ownedCount}/{totalCount} ({ownedCount * 100 / totalCount}%)";
    }

    [RelayCommand]
    private void ToggleDisplay(CollectionItem? item)
    {
        Console.WriteLine($"[ToggleDisplay] 호출됨, item: {item?.Creature?.Name ?? "null"}");

        if (item == null || !item.IsOwned)
        {
            Console.WriteLine("[ToggleDisplay] item이 null이거나 소유하지 않음");
            return;
        }

        if (item.IsInDisplay)
        {
            // 진열장에서 제거
            Console.WriteLine($"[ToggleDisplay] 진열장에서 제거: {item.Creature.Name}");
            _db.RemoveCreatureFromDisplay(item.Creature.Id);
            item.IsInDisplay = false;
        }
        else
        {
            // 빈 슬롯 찾기
            var nextSlot = _db.GetNextAvailableSlot();
            Console.WriteLine($"[ToggleDisplay] 다음 빈 슬롯: {nextSlot}");
            if (nextSlot >= 0)
            {
                _db.SetDisplaySlot(nextSlot, item.Creature.Id);
                item.IsInDisplay = true;
                Console.WriteLine($"[ToggleDisplay] 슬롯 {nextSlot}에 추가됨: {item.Creature.Name}");
            }
            else
            {
                Console.WriteLine("[ToggleDisplay] 빈 슬롯 없음!");
            }
        }
    }

    private static System.Collections.Generic.List<Creature> GetAllCreatures(DatabaseService db)
    {
        using var connection = db.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, name, rarity, sprite_path, description
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
                SpritePath = reader.GetString(3),
                Description = reader.GetString(4)
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
    private bool _isInDisplay;

    public string DisplayName => IsOwned ? Creature.Name : "???";
    public string RarityText => IsOwned ? Creature.Rarity.ToString() : "";
    public string CountText => IsOwned && Count > 1 ? $"x{Count}" : "";
    public double Opacity => IsOwned ? 1.0 : 0.3;
    public string DisplayButtonText => IsInDisplay ? "진열장에서 제거" : "진열장에 추가";
}
