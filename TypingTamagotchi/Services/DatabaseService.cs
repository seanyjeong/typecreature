using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using TypingTamagotchi.Models;

namespace TypingTamagotchi.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string dbPath = "tamagotchi.db")
    {
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS creatures (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                rarity INTEGER NOT NULL,
                sprite_path TEXT NOT NULL,
                description TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS collection (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                creature_id INTEGER NOT NULL,
                obtained_at TEXT NOT NULL,
                FOREIGN KEY (creature_id) REFERENCES creatures(id)
            );

            CREATE TABLE IF NOT EXISTS stats (
                key TEXT PRIMARY KEY,
                value INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS current_egg (
                id INTEGER PRIMARY KEY CHECK (id = 1),
                name TEXT NOT NULL,
                sprite_path TEXT NOT NULL,
                required_count INTEGER NOT NULL,
                current_count INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS display_slots (
                slot_index INTEGER PRIMARY KEY CHECK (slot_index >= 0 AND slot_index < 10),
                creature_id INTEGER NOT NULL,
                FOREIGN KEY (creature_id) REFERENCES creatures(id)
            );
        ";
        command.ExecuteNonQuery();
    }

    public List<(int slotIndex, int creatureId)> GetDisplaySlots()
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT slot_index, creature_id FROM display_slots ORDER BY slot_index";

        var slots = new List<(int, int)>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            slots.Add((reader.GetInt32(0), reader.GetInt32(1)));
        }
        return slots;
    }

    public void SetDisplaySlot(int slotIndex, int creatureId)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO display_slots (slot_index, creature_id)
            VALUES (@slot, @creature)
        ";
        command.Parameters.AddWithValue("@slot", slotIndex);
        command.Parameters.AddWithValue("@creature", creatureId);
        command.ExecuteNonQuery();
    }

    public void RemoveFromDisplaySlot(int slotIndex)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM display_slots WHERE slot_index = @slot";
        command.Parameters.AddWithValue("@slot", slotIndex);
        command.ExecuteNonQuery();
    }

    public void RemoveCreatureFromDisplay(int creatureId)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM display_slots WHERE creature_id = @creature";
        command.Parameters.AddWithValue("@creature", creatureId);
        command.ExecuteNonQuery();
    }

    public int? GetCreatureDisplaySlot(int creatureId)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT slot_index FROM display_slots WHERE creature_id = @creature";
        command.Parameters.AddWithValue("@creature", creatureId);
        var result = command.ExecuteScalar();
        return result != null ? Convert.ToInt32(result) : null;
    }

    public int GetNextAvailableSlot()
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT slot_index FROM display_slots ORDER BY slot_index";

        var usedSlots = new HashSet<int>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            usedSlots.Add(reader.GetInt32(0));
        }

        for (int i = 0; i < 10; i++)
        {
            if (!usedSlots.Contains(i)) return i;
        }
        return -1; // 모든 슬롯이 사용 중
    }

    public SqliteConnection GetConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public void SeedCreaturesIfEmpty()
    {
        using var connection = GetConnection();

        var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "SELECT COUNT(*) FROM creatures";
        var count = Convert.ToInt32(checkCommand.ExecuteScalar());

        if (count > 0) return;

        // 50종 크리처 시드 데이터 (MVP)
        var creatures = new List<(string name, Rarity rarity, string desc)>
        {
            // Common (25종)
            ("슬라임", Rarity.Common, "말랑말랑한 젤리 생물"),
            ("꼬마구름", Rarity.Common, "둥실둥실 떠다니는 구름"),
            ("잎새", Rarity.Common, "바람에 흔들리는 잎사귀"),
            ("물방울", Rarity.Common, "투명하게 빛나는 물방울"),
            ("돌멩이", Rarity.Common, "단단한 작은 돌"),
            ("별똥별", Rarity.Common, "하늘에서 떨어진 작은 별"),
            ("꽃잎", Rarity.Common, "향기로운 분홍 꽃잎"),
            ("솜뭉치", Rarity.Common, "폭신폭신한 솜"),
            ("젤리콩", Rarity.Common, "달콤한 젤리 콩"),
            ("이끼돌", Rarity.Common, "이끼가 낀 귀여운 돌"),
            ("눈송이", Rarity.Common, "차가운 눈 결정"),
            ("반딧불", Rarity.Common, "밤에 빛나는 벌레"),
            ("씨앗", Rarity.Common, "가능성이 담긴 씨앗"),
            ("조약돌", Rarity.Common, "강에서 온 매끈한 돌"),
            ("먼지토끼", Rarity.Common, "뽀송뽀송한 먼지 덩어리"),
            ("비누방울", Rarity.Common, "무지개빛 비누방울"),
            ("도토리", Rarity.Common, "다람쥐가 좋아하는 열매"),
            ("꿀방울", Rarity.Common, "달콤한 황금 방울"),
            ("깃털", Rarity.Common, "가벼운 새 깃털"),
            ("이슬", Rarity.Common, "아침에 맺힌 이슬"),
            ("모래알", Rarity.Common, "해변의 작은 모래"),
            ("풀잎", Rarity.Common, "초록빛 풀잎"),
            ("나뭇가지", Rarity.Common, "작은 나무 조각"),
            ("진흙이", Rarity.Common, "말랑한 진흙 덩어리"),
            ("버섯", Rarity.Common, "동글동글한 버섯"),

            // Rare (15종)
            ("번개토끼", Rarity.Rare, "전기를 품은 토끼"),
            ("불꽃여우", Rarity.Rare, "꼬리에서 불꽃이 피는 여우"),
            ("얼음펭귄", Rarity.Rare, "차가운 기운의 펭귄"),
            ("바람새", Rarity.Rare, "바람을 타고 나는 새"),
            ("꽃사슴", Rarity.Rare, "뿔에 꽃이 피는 사슴"),
            ("달토끼", Rarity.Rare, "달빛을 받으면 빛나는 토끼"),
            ("무지개뱀", Rarity.Rare, "일곱 색깔 비늘의 뱀"),
            ("구름고래", Rarity.Rare, "하늘을 헤엄치는 고래"),
            ("수정나비", Rarity.Rare, "투명한 날개의 나비"),
            ("숲요정", Rarity.Rare, "숲을 지키는 작은 요정"),
            ("별똥곰", Rarity.Rare, "별빛 털을 가진 곰"),
            ("파도물개", Rarity.Rare, "파도를 타는 물개"),
            ("안개늑대", Rarity.Rare, "안개 속에서 나타나는 늑대"),
            ("노을새", Rarity.Rare, "저녁노을 빛깔의 새"),
            ("이끼거북", Rarity.Rare, "등에 정원이 있는 거북"),

            // Epic (7종)
            ("용아기", Rarity.Epic, "아직 어린 용"),
            ("유니콘", Rarity.Epic, "무지개 갈기의 유니콘"),
            ("피닉스", Rarity.Epic, "불꽃에서 다시 태어나는 새"),
            ("크라켄", Rarity.Epic, "심해의 거대 문어"),
            ("그리폰", Rarity.Epic, "독수리와 사자의 합체"),
            ("켈피", Rarity.Epic, "물속의 신비한 말"),
            ("바실리스크", Rarity.Epic, "눈빛이 무서운 뱀"),

            // Legendary (3종)
            ("황금드래곤", Rarity.Legendary, "전설의 황금빛 용"),
            ("세계수정령", Rarity.Legendary, "세계수를 지키는 정령"),
            ("시간고양이", Rarity.Legendary, "시간을 다루는 신비한 고양이"),
        };

        var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = @"
            INSERT INTO creatures (name, rarity, sprite_path, description)
            VALUES (@name, @rarity, @sprite, @desc)
        ";

        for (int i = 0; i < creatures.Count; i++)
        {
            var (name, rarity, desc) = creatures[i];
            insertCommand.Parameters.Clear();
            insertCommand.Parameters.AddWithValue("@name", name);
            insertCommand.Parameters.AddWithValue("@rarity", (int)rarity);
            insertCommand.Parameters.AddWithValue("@sprite", $"Creatures/{i + 1}.png");
            insertCommand.Parameters.AddWithValue("@desc", desc);
            insertCommand.ExecuteNonQuery();
        }
    }
}
