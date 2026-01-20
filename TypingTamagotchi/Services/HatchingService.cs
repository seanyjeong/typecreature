using System;
using System.Collections.Generic;
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

    public (int keystrokes, int clicks) GetCurrentProgress()
    {
        using var connection = _db.GetConnection();

        var ksCommand = connection.CreateCommand();
        ksCommand.CommandText = "SELECT value FROM stats WHERE key = 'keystrokes'";
        var ksResult = ksCommand.ExecuteScalar();
        var keystrokes = ksResult != null ? Convert.ToInt32(ksResult) : 0;

        var clCommand = connection.CreateCommand();
        clCommand.CommandText = "SELECT value FROM stats WHERE key = 'clicks'";
        var clResult = clCommand.ExecuteScalar();
        var clicks = clResult != null ? Convert.ToInt32(clResult) : 0;

        return (keystrokes, clicks);
    }

    public void RecordInput(bool isClick)
    {
        using var connection = _db.GetConnection();
        var key = isClick ? "clicks" : "keystrokes";

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO stats (key, value) VALUES (@key, 1)
            ON CONFLICT(key) DO UPDATE SET value = value + 1
        ";
        command.Parameters.AddWithValue("@key", key);
        command.ExecuteNonQuery();
    }

    public Creature? TryHatch()
    {
        var (keystrokes, clicks) = GetCurrentProgress();
        var totalInputs = keystrokes + clicks;

        if (totalInputs < 1500) // 1.5배 더 느리게
            return null;

        // 진행 상황 리셋
        using var connection = _db.GetConnection();
        var resetCommand = connection.CreateCommand();
        resetCommand.CommandText = @"
            UPDATE stats SET value = value - 1500 WHERE key = 'keystrokes';
            UPDATE stats SET value = 0 WHERE key = 'clicks';
        ";
        resetCommand.ExecuteNonQuery();

        // 부화
        return Hatch();
    }
}
