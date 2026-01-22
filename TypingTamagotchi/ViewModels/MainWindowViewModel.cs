using System;
using System.Collections.Generic;
using Avalonia.Threading;
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
    private readonly DispatcherTimer _timeProgressTimer;
    private readonly UpdateService _updateService;
    private readonly ChangelogService _changelogService;

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

    // 업데이트 관련
    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private string _updateVersion = "";

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private int _downloadProgress;

    [ObservableProperty]
    private bool _isUpdateReady;

    // 변경 로그 관련
    [ObservableProperty]
    private bool _isChangelogVisible;

    [ObservableProperty]
    private string _changelogVersion = "";

    [ObservableProperty]
    private string _changelogTitle = "";

    [ObservableProperty]
    private string _changelogDate = "";

    [ObservableProperty]
    private List<ChangeItem> _changelogItems = new();

    public MainWindowViewModel()
    {
        _db = new DatabaseService();
        _db.SeedCreaturesIfEmpty();

        _eggService = new EggService(_db);
        _hatchingService = new HatchingService(_db);

        // 플랫폼에 맞는 입력 서비스 선택
        // useSimulated: true = 개발/테스트 모드, false = 실제 전역 후킹
        _inputService = InputServiceFactory.Create(useSimulated: false);

        _eggService.EggUpdated += OnEggUpdated;
        _eggService.EggReady += OnEggReady;
        _inputService.InputDetected += OnInputDetected;

        UpdateEggDisplay();
        UpdateCollectionStatus();

        _inputService.Start();

        // 시간 기반 부화율 증가 (초당 0.1)
        _timeProgressTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timeProgressTimer.Tick += OnTimeProgressTick;
        _timeProgressTimer.Start();

        // 업데이트 체크
        _updateService = new UpdateService();
        _updateService.UpdateAvailable += version =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                UpdateVersion = version;
                IsUpdateAvailable = true;
            });
        };
        _updateService.DownloadProgress += progress =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                DownloadProgress = progress;
            });
        };
        _updateService.UpdateReady += () =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                IsDownloading = false;
                IsUpdateReady = true;
            });
        };

        // 앱 시작 시 업데이트 체크 (백그라운드)
        _ = CheckForUpdatesAsync();

        // 변경 로그 체크 (업데이트 후 첫 실행 시)
        _changelogService = new ChangelogService(_db);
        CheckChangelog();
    }

    private void CheckChangelog()
    {
        if (_changelogService.HasNewChangelog())
        {
            var versionInfo = _changelogService.GetCurrentVersionInfo();
            if (versionInfo != null)
            {
                ChangelogVersion = versionInfo.Version ?? "";
                ChangelogTitle = versionInfo.Title ?? "";
                ChangelogDate = versionInfo.Date ?? "";
                ChangelogItems = versionInfo.Changes ?? new List<ChangeItem>();
                IsChangelogVisible = true;
            }
        }
    }

    private async System.Threading.Tasks.Task CheckForUpdatesAsync()
    {
        await _updateService.CheckForUpdatesAsync();
    }

    private void OnTimeProgressTick(object? sender, EventArgs e)
    {
        // 부화 팝업이 떠있지 않을 때만 시간 진행률 추가
        if (!IsHatchPopupVisible)
        {
            _eggService.AddProgress(0.025);
        }
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

    [RelayCommand]
    private async System.Threading.Tasks.Task DownloadUpdate()
    {
        IsDownloading = true;
        await _updateService.DownloadUpdateAsync();
    }

    [RelayCommand]
    private void ApplyUpdate()
    {
        _updateService.ApplyUpdateAndRestart();
    }

    [RelayCommand]
    private void DismissUpdate()
    {
        IsUpdateAvailable = false;
    }

    [RelayCommand]
    private void CloseChangelog()
    {
        IsChangelogVisible = false;
        _changelogService.MarkChangelogSeen();
    }
}
