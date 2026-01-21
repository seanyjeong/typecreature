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

    // 속성별 부화
    public Creature HatchByElement(Element element)
    {
        var rarity = RollRarity();
        var creature = GetRandomCreatureByRarityAndElement(rarity, element);
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
            SELECT id, name, rarity, element, sprite_path, description, age, gender, favorite_food, dislikes, background
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
                Element = (Element)reader.GetInt32(3),
                SpritePath = reader.GetString(4),
                Description = reader.GetString(5),
                Age = reader.IsDBNull(6) ? "" : reader.GetString(6),
                Gender = reader.IsDBNull(7) ? "" : reader.GetString(7),
                FavoriteFood = reader.IsDBNull(8) ? "" : reader.GetString(8),
                Dislikes = reader.IsDBNull(9) ? "" : reader.GetString(9),
                Background = reader.IsDBNull(10) ? "" : reader.GetString(10)
            };
        }

        throw new InvalidOperationException($"No creature found for rarity {rarity}");
    }

    private Creature GetRandomCreatureByRarityAndElement(Rarity rarity, Element element)
    {
        using var connection = _db.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, name, rarity, element, sprite_path, description, age, gender, favorite_food, dislikes, background
            FROM creatures
            WHERE rarity = @rarity AND element = @element
            ORDER BY RANDOM()
            LIMIT 1
        ";
        command.Parameters.AddWithValue("@rarity", (int)rarity);
        command.Parameters.AddWithValue("@element", (int)element);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new Creature
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
            };
        }

        // 해당 속성+등급 조합이 없으면 속성만으로 시도
        return GetRandomCreatureByElement(element);
    }

    private Creature GetRandomCreatureByElement(Element element)
    {
        using var connection = _db.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, name, rarity, element, sprite_path, description, age, gender, favorite_food, dislikes, background
            FROM creatures
            WHERE element = @element
            ORDER BY RANDOM()
            LIMIT 1
        ";
        command.Parameters.AddWithValue("@element", (int)element);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new Creature
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
            };
        }

        throw new InvalidOperationException($"No creature found for element {element}");
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
            SELECT c.id, c.name, c.rarity, c.element, c.sprite_path, c.description,
                   c.age, c.gender, c.favorite_food, c.dislikes, c.background,
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
                Element = (Element)reader.GetInt32(3),
                SpritePath = reader.GetString(4),
                Description = reader.GetString(5),
                Age = reader.IsDBNull(6) ? "" : reader.GetString(6),
                Gender = reader.IsDBNull(7) ? "" : reader.GetString(7),
                FavoriteFood = reader.IsDBNull(8) ? "" : reader.GetString(8),
                Dislikes = reader.IsDBNull(9) ? "" : reader.GetString(9),
                Background = reader.IsDBNull(10) ? "" : reader.GetString(10)
            };
            var count = reader.GetInt32(11);
            var firstObtained = DateTime.Parse(reader.GetString(12));
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

        if (totalInputs < 1500)
            return null;

        // 진행 상황 리셋 (음수 방지)
        using var connection = _db.GetConnection();
        var resetCommand = connection.CreateCommand();
        resetCommand.CommandText = @"
            UPDATE stats SET value = CASE WHEN value >= 1500 THEN value - 1500 ELSE 0 END WHERE key = 'keystrokes';
            UPDATE stats SET value = 0 WHERE key = 'clicks';
        ";
        resetCommand.ExecuteNonQuery();

        // 부화
        return Hatch();
    }

    // 속성별 부화 시도
    public Creature? TryHatchByElement(Element element)
    {
        var (keystrokes, clicks) = GetCurrentProgress();
        var totalInputs = keystrokes + clicks;

        if (totalInputs < 1500)
            return null;

        // 진행 상황 리셋
        using var connection = _db.GetConnection();
        var resetCommand = connection.CreateCommand();
        resetCommand.CommandText = @"
            UPDATE stats SET value = CASE WHEN value >= 1500 THEN value - 1500 ELSE 0 END WHERE key = 'keystrokes';
            UPDATE stats SET value = 0 WHERE key = 'clicks';
        ";
        resetCommand.ExecuteNonQuery();

        // 속성별 부화
        return HatchByElement(element);
    }

    // 레전더리 부화 (확정)
    public Creature HatchLegendary()
    {
        var creature = GetRandomCreatureByRarity(Rarity.Legendary);
        SaveToCollection(creature);
        CreatureHatched?.Invoke(creature);
        return creature;
    }

    // 레전더리 부화 시도
    public Creature? TryHatchLegendary()
    {
        var (keystrokes, clicks) = GetCurrentProgress();
        var totalInputs = keystrokes + clicks;

        if (totalInputs < 1500)
            return null;

        // 진행 상황 리셋
        using var connection = _db.GetConnection();
        var resetCommand = connection.CreateCommand();
        resetCommand.CommandText = @"
            UPDATE stats SET value = CASE WHEN value >= 1500 THEN value - 1500 ELSE 0 END WHERE key = 'keystrokes';
            UPDATE stats SET value = 0 WHERE key = 'clicks';
        ";
        resetCommand.ExecuteNonQuery();

        // 레전더리 부화 확정
        return HatchLegendary();
    }
}
