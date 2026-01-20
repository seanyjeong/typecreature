using System;
using System.Collections.Generic;
using System.Linq;
using TypingTamagotchi.Models;

namespace TypingTamagotchi.Services;

public class DesktopPetService
{
    private readonly DatabaseService _db;
    private readonly Random _random = new();
    private readonly List<DesktopPet> _activePets = new();
    private int _nextPetId = 1;

    public const int MaxPets = 10;
    public const int DefaultMaxPets = 5;
    public const double GreetingDistance = 80;
    public const double PetSize = 64;

    public event Action<DesktopPet>? PetAdded;
    public event Action<DesktopPet>? PetRemoved;
    public event Action<DesktopPet, DesktopPet>? PetsGreeting;

    public IReadOnlyList<DesktopPet> ActivePets => _activePets;
    public int MaxActivePets { get; set; } = DefaultMaxPets;

    public DesktopPetService(DatabaseService db)
    {
        _db = db;
        LoadActivePets();
    }

    private void LoadActivePets()
    {
        using var connection = _db.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS desktop_pets (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                creature_id INTEGER NOT NULL,
                x REAL DEFAULT 100,
                y REAL DEFAULT 100,
                FOREIGN KEY (creature_id) REFERENCES creatures(id)
            )
        ";
        command.ExecuteNonQuery();

        command.CommandText = @"
            SELECT dp.id, dp.x, dp.y, c.id, c.name, c.rarity, c.sprite_path, c.description
            FROM desktop_pets dp
            JOIN creatures c ON dp.creature_id = c.id
        ";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var pet = new DesktopPet
            {
                Id = reader.GetInt32(0),
                X = reader.GetDouble(1),
                Y = reader.GetDouble(2),
                Creature = new Creature
                {
                    Id = reader.GetInt32(3),
                    Name = reader.GetString(4),
                    Rarity = (Rarity)reader.GetInt32(5),
                    SpritePath = reader.GetString(6),
                    Description = reader.GetString(7)
                },
                State = PetState.Idle,
                FacingRight = _random.Next(2) == 0
            };
            _activePets.Add(pet);
            if (pet.Id >= _nextPetId) _nextPetId = pet.Id + 1;
        }
    }

    public bool CanAddPet() => _activePets.Count < MaxActivePets;

    public bool IsPetOnDesktop(int creatureId) => _activePets.Any(p => p.Creature.Id == creatureId);

    public DesktopPet? AddPet(Creature creature, double screenWidth, double screenHeight)
    {
        if (!CanAddPet()) return null;
        if (IsPetOnDesktop(creature.Id)) return null;

        var pet = new DesktopPet
        {
            Id = _nextPetId++,
            Creature = creature,
            X = _random.Next(100, (int)(screenWidth - 100)),
            Y = screenHeight - PetSize - 50, // 화면 하단 (태스크바 위)
            State = PetState.Idle,
            FacingRight = _random.Next(2) == 0
        };

        _activePets.Add(pet);
        SavePet(pet);
        PetAdded?.Invoke(pet);
        return pet;
    }

    public void RemovePet(DesktopPet pet)
    {
        _activePets.Remove(pet);
        DeletePet(pet);
        PetRemoved?.Invoke(pet);
    }

    public void RemovePetByCreatureId(int creatureId)
    {
        var pet = _activePets.FirstOrDefault(p => p.Creature.Id == creatureId);
        if (pet != null) RemovePet(pet);
    }

    private void SavePet(DesktopPet pet)
    {
        using var connection = _db.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO desktop_pets (creature_id, x, y)
            VALUES (@creatureId, @x, @y)
        ";
        command.Parameters.AddWithValue("@creatureId", pet.Creature.Id);
        command.Parameters.AddWithValue("@x", pet.X);
        command.Parameters.AddWithValue("@y", pet.Y);
        command.ExecuteNonQuery();
    }

    private void DeletePet(DesktopPet pet)
    {
        using var connection = _db.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM desktop_pets WHERE creature_id = @creatureId";
        command.Parameters.AddWithValue("@creatureId", pet.Creature.Id);
        command.ExecuteNonQuery();
    }

    public void UpdatePetPosition(DesktopPet pet)
    {
        using var connection = _db.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE desktop_pets SET x = @x, y = @y WHERE creature_id = @creatureId
        ";
        command.Parameters.AddWithValue("@x", pet.X);
        command.Parameters.AddWithValue("@y", pet.Y);
        command.Parameters.AddWithValue("@creatureId", pet.Creature.Id);
        command.ExecuteNonQuery();
    }

    public void UpdatePets(double deltaTime, double screenWidth, double screenHeight)
    {
        foreach (var pet in _activePets)
        {
            if (pet.State == PetState.Dragging) continue;

            pet.StateTimer -= deltaTime;
            pet.AnimationFrame += deltaTime * 8; // 애니메이션 속도

            if (pet.StateTimer <= 0)
            {
                TransitionToNewState(pet);
            }

            if (pet.State == PetState.Walking)
            {
                var speed = 30 * deltaTime;
                pet.X += pet.FacingRight ? speed : -speed;

                // 화면 경계 체크
                if (pet.X <= 10)
                {
                    pet.X = 10;
                    pet.FacingRight = true;
                }
                else if (pet.X >= screenWidth - PetSize - 10)
                {
                    pet.X = screenWidth - PetSize - 10;
                    pet.FacingRight = false;
                }
            }
        }

        CheckGreetings();
    }

    private void TransitionToNewState(DesktopPet pet)
    {
        if (pet.State == PetState.Greeting)
        {
            // 인사 후 반대 방향으로
            pet.FacingRight = !pet.FacingRight;
        }

        var roll = _random.Next(100);
        if (roll < 40)
        {
            pet.State = PetState.Idle;
            pet.StateTimer = 2 + _random.NextDouble() * 3;
        }
        else if (roll < 80)
        {
            pet.State = PetState.Walking;
            pet.StateTimer = 2 + _random.NextDouble() * 4;
            if (_random.Next(2) == 0) pet.FacingRight = !pet.FacingRight;
        }
        else
        {
            pet.State = PetState.Sitting;
            pet.StateTimer = 3 + _random.NextDouble() * 5;
        }
    }

    private void CheckGreetings()
    {
        for (int i = 0; i < _activePets.Count; i++)
        {
            for (int j = i + 1; j < _activePets.Count; j++)
            {
                var pet1 = _activePets[i];
                var pet2 = _activePets[j];

                if (pet1.State == PetState.Greeting || pet2.State == PetState.Greeting)
                    continue;
                if (pet1.State == PetState.Dragging || pet2.State == PetState.Dragging)
                    continue;

                var distance = Math.Abs(pet1.X - pet2.X);
                if (distance < GreetingDistance)
                {
                    pet1.State = PetState.Greeting;
                    pet2.State = PetState.Greeting;
                    pet1.StateTimer = 1.5;
                    pet2.StateTimer = 1.5;

                    // 서로 마주보기
                    pet1.FacingRight = pet1.X < pet2.X;
                    pet2.FacingRight = pet2.X < pet1.X;

                    PetsGreeting?.Invoke(pet1, pet2);
                }
            }
        }
    }

    public void OnPetClicked(DesktopPet pet)
    {
        if (pet.State == PetState.Dragging) return;

        pet.State = PetState.Clicked;
        pet.StateTimer = 0.5;
    }

    public void StartDragging(DesktopPet pet)
    {
        pet.State = PetState.Dragging;
    }

    public void StopDragging(DesktopPet pet, double screenHeight)
    {
        pet.State = PetState.Idle;
        pet.StateTimer = 1;
        // 바닥으로 고정
        pet.Y = screenHeight - PetSize - 50;
        UpdatePetPosition(pet);
    }

    public List<Creature> GetOwnedCreatures()
    {
        using var connection = _db.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT DISTINCT c.id, c.name, c.rarity, c.sprite_path, c.description
            FROM collection col
            JOIN creatures c ON col.creature_id = c.id
            ORDER BY c.rarity DESC, c.name
        ";

        var results = new List<Creature>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new Creature
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Rarity = (Rarity)reader.GetInt32(2),
                SpritePath = reader.GetString(3),
                Description = reader.GetString(4)
            });
        }
        return results;
    }
}
