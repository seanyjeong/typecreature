using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TypingTamagotchi.Models;
using TypingTamagotchi.Services;

namespace TypingTamagotchi.ViewModels;

public partial class PlaygroundViewModel : ViewModelBase
{
    private readonly DatabaseService _db;
    private readonly DispatcherTimer _gameTimer;
    private DateTime _lastUpdate;

    // 놀이터 크기 (초기값)
    [ObservableProperty]
    private double _playgroundWidth = 700;

    [ObservableProperty]
    private double _playgroundHeight = 280;

    // 바닥 Y 위치 (상대적)
    public double GroundY => PlaygroundHeight - 60;

    // 크리처들
    [ObservableProperty]
    private ObservableCollection<PlaygroundCreature> _creatures = new();

    // 이펙트
    [ObservableProperty]
    private ObservableCollection<BumpEffect> _effects = new();


    public PlaygroundViewModel()
    {
        _db = new DatabaseService();
        LoadCreatures();

        // 게임 루프 타이머 (60fps)
        _gameTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _gameTimer.Tick += OnGameTick;
        _lastUpdate = DateTime.Now;
        _gameTimer.Start();
    }

    private void LoadCreatures()
    {
        var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "playground_debug.log");
        var log = new System.Collections.Generic.List<string>();

        var playgroundData = _db.GetPlaygroundCreatures();
        log.Add($"[{DateTime.Now}] LoadCreatures: {playgroundData.Count} creatures in DB");
        Creatures.Clear();

        foreach (var (slot, creatureId) in playgroundData)
        {
            log.Add($"Loading slot {slot}, creatureId {creatureId}");
            var creature = GetCreatureById(creatureId);
            if (creature != null)
            {
                log.Add($"Found creature: {creature.Name}, sprite: {creature.SpritePath}");
                var pc = new PlaygroundCreature
                {
                    Creature = creature,
                    X = Random.Shared.NextDouble() * (PlaygroundWidth - 60) + 10,
                    Y = GroundY,
                    VelocityX = (Random.Shared.NextDouble() - 0.5) * PlaygroundCreature.MoveSpeed * 2,
                    Direction = Random.Shared.NextDouble() > 0.5 ? 1 : -1
                };
                pc.GroundY = GroundY;
                Creatures.Add(pc);
                log.Add($"Added at X={pc.X:F1}, Y={pc.Y:F1}, GroundY={pc.GroundY:F1}");
            }
            else
            {
                log.Add($"*** Creature NOT FOUND for id {creatureId} ***");
            }
        }
        log.Add($"Total creatures loaded: {Creatures.Count}");
        log.Add($"PlaygroundWidth={PlaygroundWidth}, PlaygroundHeight={PlaygroundHeight}, GroundY={GroundY}");

        System.IO.File.WriteAllLines(logPath, log);
    }

    private void OnGameTick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        var deltaTime = (now - _lastUpdate).TotalSeconds;
        _lastUpdate = now;

        // 델타 타임 제한 (프레임 드롭 방지)
        deltaTime = Math.Min(deltaTime, 0.05);

        // 각 크리처 업데이트
        foreach (var creature in Creatures)
        {
            creature.Update(deltaTime, 10, PlaygroundWidth - 10, GroundY);
        }

        // 충돌 검사
        CheckCollisions();

        // 이펙트 업데이트
        UpdateEffects(deltaTime);
    }

    private void CheckCollisions()
    {
        var creatureList = Creatures.ToList();

        for (int i = 0; i < creatureList.Count; i++)
        {
            for (int j = i + 1; j < creatureList.Count; j++)
            {
                var a = creatureList[i];
                var b = creatureList[j];

                // 넘어져 있으면 충돌 무시
                if (a.IsKnockedOver || b.IsKnockedOver) continue;

                // 충돌 박스 체크
                var aCenterX = a.X + PlaygroundCreature.Width / 2;
                var bCenterX = b.X + PlaygroundCreature.Width / 2;
                var aCenterY = a.Y - PlaygroundCreature.Height / 2;
                var bCenterY = b.Y - PlaygroundCreature.Height / 2;

                var dx = Math.Abs(aCenterX - bCenterX);
                var dy = Math.Abs(aCenterY - bCenterY);

                // 충돌 감지 (겹침 방지)
                if (dx < PlaygroundCreature.Width * 0.6 && dy < PlaygroundCreature.Height * 0.8)
                {
                    // 위에서 밟기 (점프 중인 크리처가 다른 크리처를 밟음)
                    if (a.VelocityY > 0 && aCenterY < bCenterY - 20 && !a.IsOnGround)
                    {
                        b.Squash();
                        a.BounceUp();
                        SpawnEffect((aCenterX + bCenterX) / 2, bCenterY);
                    }
                    else if (b.VelocityY > 0 && bCenterY < aCenterY - 20 && !b.IsOnGround)
                    {
                        a.Squash();
                        b.BounceUp();
                        SpawnEffect((aCenterX + bCenterX) / 2, aCenterY);
                    }
                    // 옆에서 부딪힘 - 겹치지 않게 밀어내고 방향 전환
                    else
                    {
                        // 겹침 해제 - 서로 밀어냄
                        double overlap = PlaygroundCreature.Width * 0.6 - dx;
                        if (overlap > 0)
                        {
                            double push = overlap / 2 + 2;
                            if (a.X < b.X)
                            {
                                a.X -= push;
                                b.X += push;
                            }
                            else
                            {
                                a.X += push;
                                b.X -= push;
                            }
                        }

                        // 방향 전환 (서로 반대로)
                        if (a.X < b.X)
                        {
                            a.VelocityX = -Math.Abs(a.VelocityX) - 20;
                            b.VelocityX = Math.Abs(b.VelocityX) + 20;
                            a.Direction = -1;
                            b.Direction = 1;
                        }
                        else
                        {
                            a.VelocityX = Math.Abs(a.VelocityX) + 20;
                            b.VelocityX = -Math.Abs(b.VelocityX) - 20;
                            a.Direction = 1;
                            b.Direction = -1;
                        }

                        // 가끔 한 마리가 점프해서 넘어감 (30% 확률)
                        if (Random.Shared.NextDouble() < 0.3)
                        {
                            if (Random.Shared.NextDouble() < 0.5)
                                a.Jump();
                            else
                                b.Jump();
                        }
                    }
                }
            }
        }
    }

    private void SpawnEffect(double x, double y)
    {
        var effect = new BumpEffect
        {
            X = x - 32, // 이펙트 중심 맞추기
            Y = y - 32,
            Lifetime = 0.5
        };
        Effects.Add(effect);
    }

    private void UpdateEffects(double deltaTime)
    {
        var toRemove = Effects.Where(e =>
        {
            e.Lifetime -= deltaTime;
            e.Opacity = Math.Max(0, e.Lifetime / 0.5);
            return e.Lifetime <= 0;
        }).ToList();

        foreach (var e in toRemove)
        {
            Effects.Remove(e);
        }
    }

    public void Refresh()
    {
        LoadCreatures();
    }

    [RelayCommand]
    private void ClearAll()
    {
        _db.ClearAllFromPlayground();
        Creatures.Clear();
        CollectionViewModel.NotifyPlaygroundChanged();
    }

    public void Stop()
    {
        _gameTimer.Stop();
    }

    private Creature? GetCreatureById(int id)
    {
        using var connection = _db.GetConnection();
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, name, rarity, element, sprite_path, description
            FROM creatures WHERE id = @id
        ";
        command.Parameters.AddWithValue("@id", id);

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
                Description = reader.GetString(5)
            };
        }
        return null;
    }
}

public partial class BumpEffect : ObservableObject
{
    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private double _opacity = 1.0;

    public double Lifetime { get; set; }
}
