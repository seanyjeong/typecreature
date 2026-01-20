using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TypingTamagotchi.Models;
using TypingTamagotchi.Services;

namespace TypingTamagotchi.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly DatabaseService _db;
    private readonly EggService _eggService;
    private readonly HatchingService _hatchingService;
    private readonly IInputService _inputService;

    public event Action? OpenCollectionRequested;
    public event Action<bool>? ToggleWidgetRequested;

    [ObservableProperty]
    private string _eggName = "";

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _progressText = "0%";

    [ObservableProperty]
    private bool _isHatchPopupVisible;

    [ObservableProperty]
    private Creature? _hatchedCreature;

    [ObservableProperty]
    private string _collectionStatus = "0/50";

    [ObservableProperty]
    private bool _isWidgetVisible;

    public MainWindowViewModel()
    {
        _db = new DatabaseService();
        _db.SeedCreaturesIfEmpty();

        _eggService = new EggService(_db);
        _hatchingService = new HatchingService(_db);
        _inputService = new SimulatedInputService();

        _eggService.EggUpdated += OnEggUpdated;
        _eggService.EggReady += OnEggReady;
        _inputService.InputDetected += OnInputDetected;

        UpdateEggDisplay();
        UpdateCollectionStatus();

        _inputService.Start();
    }

    private void OnInputDetected()
    {
        _eggService.AddProgress(1);
    }

    private void OnEggUpdated(Egg egg)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            UpdateEggDisplay();
        });
    }

    private void OnEggReady(Egg egg)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _inputService.Stop();
            var creature = _hatchingService.Hatch();
            HatchedCreature = creature;
            IsHatchPopupVisible = true;
            UpdateCollectionStatus();
        });
    }

    private void UpdateEggDisplay()
    {
        var egg = _eggService.CurrentEgg;
        EggName = egg.Name;
        Progress = egg.Progress;
        ProgressText = $"{(int)(egg.Progress * 100)}%";
    }

    private void UpdateCollectionStatus()
    {
        var owned = _hatchingService.GetOwnedCreatureCount();
        var total = _hatchingService.GetTotalCreatureCount();
        CollectionStatus = $"{owned}/{total}";
    }

    [RelayCommand]
    private void CloseHatchPopup()
    {
        IsHatchPopupVisible = false;
        _eggService.CreateNewEgg();
        _inputService.Start();
    }

    [RelayCommand]
    private void OpenCollection()
    {
        OpenCollectionRequested?.Invoke();
    }

    [RelayCommand]
    private void ToggleWidget()
    {
        IsWidgetVisible = !IsWidgetVisible;
        ToggleWidgetRequested?.Invoke(IsWidgetVisible);
    }
}
