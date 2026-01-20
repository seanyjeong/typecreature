using System;
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
