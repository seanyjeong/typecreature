using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TypingTamagotchi.Models;
using TypingTamagotchi.Services;

namespace TypingTamagotchi.ViewModels;

public partial class CollectionViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<CollectionItem> _items = new();

    [ObservableProperty]
    private string _collectionStatus = "";

    [ObservableProperty]
    private CollectionItem? _selectedItem;

    public CollectionViewModel()
    {
        LoadCollection();
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

    public string DisplayName => IsOwned ? Creature.Name : "???";
    public string RarityText => IsOwned ? Creature.Rarity.ToString() : "";
    public string CountText => IsOwned && Count > 1 ? $"x{Count}" : "";
    public double Opacity => IsOwned ? 1.0 : 0.3;
}
