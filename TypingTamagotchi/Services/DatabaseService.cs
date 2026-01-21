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
                element INTEGER NOT NULL DEFAULT 0,
                sprite_path TEXT NOT NULL,
                description TEXT NOT NULL,
                age TEXT DEFAULT '',
                gender TEXT DEFAULT '',
                favorite_food TEXT DEFAULT '',
                dislikes TEXT DEFAULT '',
                background TEXT DEFAULT ''
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
                slot_index INTEGER PRIMARY KEY CHECK (slot_index >= 0 AND slot_index < 12),
                creature_id INTEGER NOT NULL,
                FOREIGN KEY (creature_id) REFERENCES creatures(id)
            );

            CREATE TABLE IF NOT EXISTS playground_creatures (
                slot INTEGER PRIMARY KEY CHECK (slot >= 0 AND slot < 4),
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

        for (int i = 0; i < 12; i++)
        {
            if (!usedSlots.Contains(i)) return i;
        }
        return -1;
    }

    // === Playground 관련 메서드 ===

    public List<(int slot, int creatureId)> GetPlaygroundCreatures()
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT slot, creature_id FROM playground_creatures ORDER BY slot";

        var creatures = new List<(int, int)>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            creatures.Add((reader.GetInt32(0), reader.GetInt32(1)));
        }
        return creatures;
    }

    public void AddToPlayground(int creatureId)
    {
        using var connection = GetConnection();

        // 빈 슬롯 찾기
        var findCommand = connection.CreateCommand();
        findCommand.CommandText = "SELECT slot FROM playground_creatures ORDER BY slot";
        var usedSlots = new HashSet<int>();
        using (var reader = findCommand.ExecuteReader())
        {
            while (reader.Read())
            {
                usedSlots.Add(reader.GetInt32(0));
            }
        }

        int nextSlot = -1;
        for (int i = 0; i < 4; i++)
        {
            if (!usedSlots.Contains(i))
            {
                nextSlot = i;
                break;
            }
        }

        if (nextSlot < 0) return; // 슬롯 가득 참

        var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO playground_creatures (slot, creature_id) VALUES (@slot, @creature)";
        command.Parameters.AddWithValue("@slot", nextSlot);
        command.Parameters.AddWithValue("@creature", creatureId);
        command.ExecuteNonQuery();
    }

    public void RemoveFromPlayground(int creatureId)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM playground_creatures WHERE creature_id = @creature";
        command.Parameters.AddWithValue("@creature", creatureId);
        command.ExecuteNonQuery();
    }

    public bool IsInPlayground(int creatureId)
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM playground_creatures WHERE creature_id = @creature";
        command.Parameters.AddWithValue("@creature", creatureId);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public int GetPlaygroundCount()
    {
        using var connection = GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM playground_creatures";
        return Convert.ToInt32(command.ExecuteScalar());
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

        // 50종 크리처 데이터 (name, rarity, element, desc, age, gender, food, dislikes, background)
        var creatures = new List<(string name, Rarity rarity, Element element, string desc, string age, string gender, string food, string dislikes, string background)>
        {
            // Common (25종) - ID 1~25
            ("슬라임", Rarity.Common, Element.Water, "말랑말랑한 젤리 생물",
                "3살", "무성", "설탕물", "건조한 날씨",
                "숲속 연못가에서 자연발생한 슬라임. 비 오는 날을 좋아하며, 항상 촉촉하게 유지하려고 노력한다."),

            ("꼬마구름", Rarity.Common, Element.Wind, "둥실둥실 떠다니는 구름",
                "1살", "무성", "수증기", "강한 바람",
                "맑은 날 하늘에서 태어난 작은 구름. 혼자 떠다니다가 외로워서 땅으로 내려왔다."),

            ("잎새", Rarity.Common, Element.Earth, "바람에 흔들리는 잎사귀",
                "1살", "여자", "햇빛", "가을",
                "봄에 태어난 새싹이 의지를 갖게 되었다. 광합성으로 에너지를 얻으며 항상 밝은 성격이다."),

            ("물방울", Rarity.Common, Element.Water, "투명하게 빛나는 물방울",
                "1살", "무성", "이슬", "더운 날",
                "아침 이슬에서 태어나 매일 새롭게 다시 태어난다. 순수하고 깨끗한 마음을 가졌다."),

            ("돌멩이", Rarity.Common, Element.Earth, "단단한 작은 돌",
                "100살", "남자", "미네랄", "물",
                "산에서 굴러온 오래된 돌. 과묵하지만 든든하고 믿음직한 성격이다."),

            ("별똥별", Rarity.Common, Element.Fire, "하늘에서 떨어진 작은 별",
                "???", "무성", "우주먼지", "어둠",
                "밤하늘에서 떨어진 별의 조각. 아직도 희미하게 빛나며 소원을 들어준다는 전설이 있다."),

            ("꽃잎", Rarity.Common, Element.Earth, "향기로운 분홍 꽃잎",
                "1살", "여자", "꿀", "벌레",
                "정원에서 가장 예쁜 꽃에서 떨어진 꽃잎. 향기로 친구들을 기분 좋게 해준다."),

            ("솜뭉치", Rarity.Common, Element.Wind, "폭신폭신한 솜",
                "2살", "여자", "목화씨", "불",
                "목화밭에서 태어난 솜뭉치. 누구든 안아주면 기분이 좋아지는 마법을 가졌다."),

            ("젤리콩", Rarity.Common, Element.Lightning, "달콤한 젤리 콩",
                "1살", "남자", "과일즙", "쓴맛",
                "사탕가게에서 마법이 깃들어 살아난 젤리. 달콤한 농담으로 모두를 웃게 만든다."),

            ("이끼돌", Rarity.Common, Element.Earth, "이끼가 낀 귀여운 돌",
                "50살", "남자", "빗물", "건조함",
                "숲속 개울가에서 오래 살아온 돌. 이끼와 공생하며 자연의 지혜를 간직하고 있다."),

            ("눈송이", Rarity.Common, Element.Water, "차가운 눈 결정",
                "1살", "무성", "찬공기", "봄",
                "겨울 첫눈에서 태어난 눈 결정. 녹지 않는 마법으로 사계절 친구들과 함께한다."),

            ("반딧불", Rarity.Common, Element.Fire, "밤에 빛나는 벌레",
                "1살", "남자", "이슬", "낮",
                "여름밤 풀숲에서 태어났다. 어두운 곳을 밝히는 것이 사명이라고 생각한다."),

            ("씨앗", Rarity.Common, Element.Earth, "가능성이 담긴 씨앗",
                "???", "무성", "물", "콘크리트",
                "어떤 나무가 될지 아직 모르는 씨앗. 무한한 가능성을 품고 미래를 꿈꾼다."),

            ("조약돌", Rarity.Common, Element.Earth, "강에서 온 매끈한 돌",
                "200살", "무성", "강물", "높은곳",
                "오랜 시간 강물에 씻겨 둥글어졌다. 인내심이 강하고 차분한 성격이다."),

            ("먼지토끼", Rarity.Common, Element.Wind, "뽀송뽀송한 먼지 덩어리",
                "1살", "여자", "먼지", "청소기",
                "침대 밑에서 태어난 먼지 덩어리. 수줍음이 많아 구석에 숨어 지낸다."),

            ("비누방울", Rarity.Common, Element.Water, "무지개빛 비누방울",
                "1살", "무성", "비눗물", "바늘",
                "아이의 웃음에서 태어난 비눗물. 터지지 않는 특별한 방울이 되어 영원히 날아다닌다."),

            ("도토리", Rarity.Common, Element.Earth, "다람쥐가 좋아하는 열매",
                "1살", "남자", "흙", "다람쥐",
                "참나무에서 떨어진 도토리. 언젠가 큰 나무가 되겠다는 꿈을 품고 있다."),

            ("꿀방울", Rarity.Common, Element.Fire, "달콤한 황금 방울",
                "1살", "무성", "꽃꿀", "개미",
                "꿀벌이 정성껏 만든 꿀 한 방울. 달콤함으로 지친 이들을 위로한다."),

            ("깃털", Rarity.Common, Element.Wind, "가벼운 새 깃털",
                "1살", "여자", "바람", "비",
                "파랑새가 선물한 깃털. 가볍게 날아다니며 행운을 전해준다."),

            ("이슬", Rarity.Common, Element.Water, "아침에 맺힌 이슬",
                "1살", "무성", "안개", "정오",
                "새벽에 풀잎 위에 맺힌 이슬. 순수함의 상징으로 치유의 능력이 있다."),

            ("모래알", Rarity.Common, Element.Earth, "해변의 작은 모래",
                "1000살", "무성", "파도", "시멘트",
                "오래전 조개껍데기였던 모래. 바다의 추억을 간직하고 있다."),

            ("풀잎", Rarity.Common, Element.Earth, "초록빛 풀잎",
                "1살", "남자", "이슬", "제초제",
                "들판에서 가장 씩씩하게 자란 풀. 작지만 강인한 생명력을 자랑한다."),

            ("나뭇가지", Rarity.Common, Element.Earth, "작은 나무 조각",
                "5살", "남자", "빗물", "도끼",
                "큰 나무에서 떨어진 작은 가지. 언젠가 다시 뿌리내리겠다는 희망을 품고 있다."),

            ("진흙이", Rarity.Common, Element.Earth, "말랑한 진흙 덩어리",
                "???", "무성", "빗물", "가뭄",
                "비 온 뒤 웅덩이에서 태어났다. 어떤 모양이든 될 수 있는 무한한 가능성의 존재다."),

            ("버섯", Rarity.Common, Element.Earth, "동글동글한 버섯",
                "1살", "남자", "습기", "햇빛",
                "숲속 그늘에서 자란 버섯. 수줍음이 많지만 친구들에게는 따뜻한 존재다."),

            // Rare (15종) - ID 26~40
            ("번개토끼", Rarity.Rare, Element.Lightning, "전기를 품은 토끼",
                "2살", "남자", "당근", "물",
                "폭풍우 치는 밤에 번개를 맞고 태어났다. 빠른 속도와 전기 능력을 가졌다."),

            ("불꽃여우", Rarity.Rare, Element.Fire, "꼬리에서 불꽃이 피는 여우",
                "5살", "여자", "고구마", "비",
                "화산 근처에서 태어난 여우. 따뜻한 마음씨로 추운 겨울에 친구들을 따뜻하게 해준다."),

            ("얼음펭귄", Rarity.Rare, Element.Water, "차가운 기운의 펭귄",
                "4살", "남자", "생선", "더위",
                "남극에서 온 펭귄. 어디서든 시원한 환경을 만들어내며 여름에 인기가 많다."),

            ("바람새", Rarity.Rare, Element.Wind, "바람을 타고 나는 새",
                "3살", "여자", "씨앗", "새장",
                "산꼭대기에서 태어난 새. 자유로운 영혼으로 바람과 대화할 수 있다."),

            ("꽃사슴", Rarity.Rare, Element.Earth, "뿔에 꽃이 피는 사슴",
                "7살", "여자", "과일", "겨울",
                "봄의 정원에서 태어났다. 지나가는 곳마다 꽃이 피어나는 능력이 있다."),

            ("달토끼", Rarity.Rare, Element.Lightning, "달빛을 받으면 빛나는 토끼",
                "100살", "여자", "떡", "구름",
                "달에서 내려온 전설의 토끼. 보름달이 뜨면 특별한 힘이 생긴다."),

            ("무지개뱀", Rarity.Rare, Element.Lightning, "일곱 색깔 비늘의 뱀",
                "8살", "무성", "과일", "어둠",
                "비 갠 뒤 무지개 아래에서 태어났다. 비늘 색깔로 감정을 표현한다."),

            ("구름고래", Rarity.Rare, Element.Water, "하늘을 헤엄치는 고래",
                "50살", "남자", "비구름", "번개",
                "하늘 높이 떠다니는 신비한 고래. 비를 내려주며 대지를 적셔준다."),

            ("수정나비", Rarity.Rare, Element.Lightning, "투명한 날개의 나비",
                "1살", "여자", "꽃꿀", "먼지",
                "수정동굴에서 태어난 나비. 날개에서 빛이 굴절되어 무지개가 생긴다."),

            ("숲요정", Rarity.Rare, Element.Earth, "숲을 지키는 작은 요정",
                "300살", "여자", "이슬", "오염",
                "고대 숲에서 태어난 요정. 작지만 숲의 모든 생명을 보살피는 수호자다."),

            ("별똥곰", Rarity.Rare, Element.Lightning, "별빛 털을 가진 곰",
                "10살", "남자", "꿀", "시끄러움",
                "유성우가 쏟아지던 밤에 태어났다. 털에서 은은한 별빛이 난다."),

            ("파도물개", Rarity.Rare, Element.Water, "파도를 타는 물개",
                "6살", "남자", "조개", "기름",
                "깊은 바다에서 온 물개. 파도를 자유자재로 다루며 서핑의 달인이다."),

            ("안개늑대", Rarity.Rare, Element.Wind, "안개 속에서 나타나는 늑대",
                "15살", "남자", "고기", "강한빛",
                "깊은 산 안개 속에서 태어났다. 신비로운 분위기를 풍기지만 사실 친근하다."),

            ("노을새", Rarity.Rare, Element.Fire, "저녁노을 빛깔의 새",
                "4살", "여자", "열매", "밤",
                "해질녘에만 나타나는 새. 노을의 아름다움을 품고 있어 보는 이를 감동시킨다."),

            ("이끼거북", Rarity.Rare, Element.Earth, "등에 정원이 있는 거북",
                "500살", "남자", "채소", "서두름",
                "천년을 살아온 거북. 등에는 작은 생태계가 있어 많은 생물이 살고 있다."),

            // Epic (7종) - ID 41~47
            ("용아기", Rarity.Epic, Element.Fire, "아직 어린 용",
                "50살", "남자", "보석", "물",
                "화산 깊은 곳에서 태어난 아기 용. 아직 어리지만 언젠가 위대한 용이 될 것이다."),

            ("유니콘", Rarity.Epic, Element.Lightning, "무지개 갈기의 유니콘",
                "200살", "여자", "황금사과", "거짓말",
                "순수한 마음을 가진 자에게만 보이는 전설의 말. 뿔에는 치유의 힘이 있다."),

            ("피닉스", Rarity.Epic, Element.Fire, "불꽃에서 다시 태어나는 새",
                "999살", "무성", "태양열", "어둠",
                "불멸의 새. 재가 되어도 다시 태어나며, 눈물은 모든 상처를 치유한다."),

            ("크라켄", Rarity.Epic, Element.Water, "심해의 거대 문어",
                "1000살", "남자", "배", "육지",
                "심해에서 전설로만 전해지던 존재. 사실은 외로워서 친구를 찾고 있다."),

            ("그리폰", Rarity.Epic, Element.Wind, "독수리와 사자의 합체",
                "150살", "남자", "고기", "새장",
                "하늘과 땅의 왕이 합쳐진 존재. 용맹하지만 정의로운 성격이다."),

            ("켈피", Rarity.Epic, Element.Water, "물속의 신비한 말",
                "???", "여자", "해초", "불",
                "호수 깊은 곳에 사는 물의 정령. 아름답지만 신비로운 힘을 숨기고 있다."),

            ("바실리스크", Rarity.Epic, Element.Earth, "눈빛이 무서운 뱀",
                "800살", "남자", "돌", "거울",
                "고대 유적에서 깨어난 뱀. 무서운 전설과 달리 사실은 수줍음이 많다."),

            // Legendary (3종) - ID 48~50
            ("황금드래곤", Rarity.Legendary, Element.Fire, "전설의 황금빛 용",
                "10000살", "남자", "황금", "탐욕",
                "태초부터 존재했던 전설의 용. 황금빛 비늘은 지혜와 영원을 상징한다. 세상의 균형을 지키는 수호자다."),

            ("세계수정령", Rarity.Legendary, Element.Earth, "세계수를 지키는 정령",
                "999살", "무성", "생명력", "파괴",
                "세상 모든 생명의 근원인 세계수에서 태어났다. 자연의 모든 힘을 다룰 수 있으며, 생명을 창조하고 치유한다."),

            ("시간고양이", Rarity.Legendary, Element.Lightning, "시간을 다루는 신비한 고양이",
                "???", "여자", "별빛", "소음",
                "시간의 틈새에서 태어난 고양이. 과거와 미래를 볼 수 있으며, 가끔 시간을 멈추고 낮잠을 잔다."),
        };

        var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = @"
            INSERT INTO creatures (name, rarity, element, sprite_path, description, age, gender, favorite_food, dislikes, background)
            VALUES (@name, @rarity, @element, @sprite, @desc, @age, @gender, @food, @dislikes, @background)
        ";

        for (int i = 0; i < creatures.Count; i++)
        {
            var c = creatures[i];
            insertCommand.Parameters.Clear();
            insertCommand.Parameters.AddWithValue("@name", c.name);
            insertCommand.Parameters.AddWithValue("@rarity", (int)c.rarity);
            insertCommand.Parameters.AddWithValue("@element", (int)c.element);
            insertCommand.Parameters.AddWithValue("@sprite", $"Creatures/{i + 1}.png");
            insertCommand.Parameters.AddWithValue("@desc", c.desc);
            insertCommand.Parameters.AddWithValue("@age", c.age);
            insertCommand.Parameters.AddWithValue("@gender", c.gender);
            insertCommand.Parameters.AddWithValue("@food", c.food);
            insertCommand.Parameters.AddWithValue("@dislikes", c.dislikes);
            insertCommand.Parameters.AddWithValue("@background", c.background);
            insertCommand.ExecuteNonQuery();
        }
    }
}
