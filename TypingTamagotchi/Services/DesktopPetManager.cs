using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Threading;
using TypingTamagotchi.Models;
using TypingTamagotchi.ViewModels;
using TypingTamagotchi.Views;

namespace TypingTamagotchi.Services;

public class DesktopPetManager
{
    private static void Log(string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
        Console.WriteLine(line);
        try { File.AppendAllText("pet_debug.log", line + "\n"); } catch { }
    }

    private readonly DesktopPetService _petService;
    private readonly Dictionary<int, DesktopPetWindow> _petWindows = new();
    private readonly DispatcherTimer _updateTimer;
    private DateTime _lastUpdate = DateTime.Now;
    private bool _isVisible = true;

    public DesktopPetService PetService => _petService;
    public bool IsVisible => _isVisible;

    public DesktopPetManager(DatabaseService db)
    {
        _petService = new DesktopPetService(db);
        _petService.PetAdded += OnPetAdded;
        _petService.PetRemoved += OnPetRemoved;

        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(33) // ~30fps for logic
        };
        _updateTimer.Tick += OnUpdateTick;
    }

    public void Start()
    {
        Log($"Start() called, ActivePets count = {_petService.ActivePets.Count}");
        // 기존에 저장된 펫들의 윈도우 생성
        foreach (var pet in _petService.ActivePets)
        {
            CreatePetWindow(pet);
        }
        _updateTimer.Start();
        Log("Timer started");
    }

    public void Stop()
    {
        _updateTimer.Stop();
        foreach (var window in _petWindows.Values)
        {
            window.Close();
        }
        _petWindows.Clear();
    }

    private void OnUpdateTick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        var deltaTime = (now - _lastUpdate).TotalSeconds;
        _lastUpdate = now;

        var screen = GetPrimaryScreenBounds();
        _petService.UpdatePets(deltaTime, screen.Width, screen.Height);
    }

    private void OnPetAdded(DesktopPet pet)
    {
        Dispatcher.UIThread.Post(() => CreatePetWindow(pet));
    }

    private void OnPetRemoved(DesktopPet pet)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_petWindows.TryGetValue(pet.Creature.Id, out var window))
            {
                window.Close();
                _petWindows.Remove(pet.Creature.Id);
            }
        });
    }

    private void CreatePetWindow(DesktopPet pet)
    {
        Log($"CreatePetWindow: {pet.Creature.Name} (ID={pet.Creature.Id})");

        if (_petWindows.ContainsKey(pet.Creature.Id))
        {
            Log($"Window already exists for {pet.Creature.Name}");
            return;
        }

        try
        {
            var viewModel = new DesktopPetViewModel(_petService, pet);
            Log($"ViewModel created for {pet.Creature.Name}");

            var window = new DesktopPetWindow
            {
                DataContext = viewModel
            };
            Log($"Window created for {pet.Creature.Name}");

            viewModel.RemoveRequested += () =>
            {
                _petWindows.Remove(pet.Creature.Id);
            };

            _petWindows[pet.Creature.Id] = window;

            Log($"Position=({pet.X}, {pet.Y}), isVisible={_isVisible}");

            if (_isVisible)
            {
                window.Show();
                Log($"Window.Show() called for {pet.Creature.Name}");
            }
        }
        catch (Exception ex)
        {
            Log($"ERROR creating window: {ex.Message}");
        }
    }

    public void ToggleVisibility()
    {
        _isVisible = !_isVisible;

        foreach (var window in _petWindows.Values)
        {
            if (_isVisible)
                window.Show();
            else
                window.Hide();
        }
    }

    public void ShowAll()
    {
        _isVisible = true;
        foreach (var window in _petWindows.Values)
        {
            window.Show();
        }
    }

    public void HideAll()
    {
        _isVisible = false;
        foreach (var window in _petWindows.Values)
        {
            window.Hide();
        }
    }

    public DesktopPet? AddPetToDesktop(Creature creature)
    {
        Log($"AddPetToDesktop: {creature.Name} (ID={creature.Id})");
        var screen = GetPrimaryScreenBounds();
        var pet = _petService.AddPet(creature, screen.Width, screen.Height);
        Log($"AddPet result: {(pet != null ? "success" : "null")}");
        return pet;
    }

    public void RemovePetFromDesktop(int creatureId)
    {
        _petService.RemovePetByCreatureId(creatureId);
    }

    public bool IsPetOnDesktop(int creatureId) => _petService.IsPetOnDesktop(creatureId);

    public bool CanAddPet() => _petService.CanAddPet();

    public int ActivePetCount => _petService.ActivePets.Count;

    public int MaxPets => _petService.MaxActivePets;

    private (double Width, double Height) GetPrimaryScreenBounds()
    {
        // 기본값 (실제 화면 크기는 윈도우에서 가져옴)
        return (1920, 1080);
    }
}
