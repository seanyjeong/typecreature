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
    private DesktopPetManager? _petManager;

    [ObservableProperty]
    private ObservableCollection<CollectionItem> _items = new();

    [ObservableProperty]
    private string _collectionStatus = "";

    [ObservableProperty]
    private string _petStatus = "";

    [ObservableProperty]
    private CollectionItem? _selectedItem;

    public CollectionViewModel()
    {
        // App에서 전역 PetManager 가져오기
        _petManager = App.PetManager;
        LoadCollection();
        if (_petManager != null)
        {
            UpdatePetStatus();
            RefreshDesktopStatus();
        }
    }

    public void SetPetManager(DesktopPetManager petManager)
    {
        _petManager = petManager;
        UpdatePetStatus();
        RefreshDesktopStatus();
    }

    private void UpdatePetStatus()
    {
        if (_petManager == null) return;
        PetStatus = $"바탕화면: {_petManager.ActivePetCount}/{_petManager.MaxPets}";
    }

    private void RefreshDesktopStatus()
    {
        if (_petManager == null) return;
        foreach (var item in Items)
        {
            item.IsOnDesktop = _petManager.IsPetOnDesktop(item.Creature.Id);
        }
    }

    private void LoadCollection()
    {
        var db = new DatabaseService();
        var hatching = new HatchingService(db);

        // 모든 크리처 가져오기
        var allCreatures = GetAllCreatures(db);

        // 획득한 크리처 정보
        var collection = hatching.GetCollection();
        var ownedIds = collection.Select(c => c.creature.Id).ToHashSet();

        Items.Clear();
        foreach (var creature in allCreatures)
        {
            var owned = collection.FirstOrDefault(c => c.creature.Id == creature.Id);
            Items.Add(new CollectionItem
            {
                Creature = creature,
                IsOwned = ownedIds.Contains(creature.Id),
                Count = owned.count,
                FirstObtained = owned.firstObtained
            });
        }

        var ownedCount = hatching.GetOwnedCreatureCount();
        var totalCount = hatching.GetTotalCreatureCount();
        CollectionStatus = $"수집: {ownedCount}/{totalCount} ({ownedCount * 100 / totalCount}%)";
    }

    [RelayCommand]
    private void ToggleDesktop(CollectionItem? item)
    {
        if (item == null || !item.IsOwned || _petManager == null) return;

        if (item.IsOnDesktop)
        {
            _petManager.RemovePetFromDesktop(item.Creature.Id);
            item.IsOnDesktop = false;
        }
        else
        {
            if (!_petManager.CanAddPet())
            {
                // 최대 개수 초과 - 나중에 알림 추가 가능
                return;
            }
            var pet = _petManager.AddPetToDesktop(item.Creature);
            if (pet != null)
            {
                item.IsOnDesktop = true;
            }
        }
        UpdatePetStatus();
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
    private bool _isOnDesktop;

    public string DisplayName => IsOwned ? Creature.Name : "???";
    public string RarityText => IsOwned ? Creature.Rarity.ToString() : "";
    public string CountText => IsOwned && Count > 1 ? $"x{Count}" : "";
    public double Opacity => IsOwned ? 1.0 : 0.3;
    public string DesktopButtonText => IsOnDesktop ? "돌려놓기" : "바탕화면에 꺼내기";
    public bool CanToggleDesktop => IsOwned;
}
