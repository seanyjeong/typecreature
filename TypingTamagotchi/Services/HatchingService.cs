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
}
