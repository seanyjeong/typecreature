using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using TypingTamagotchi.ViewModels;

namespace TypingTamagotchi.Views;

public partial class DesktopPetWindow : Window
{
    private DesktopPetViewModel? _viewModel;
    private bool _isDragging;
    private Point _dragStart;
    private Image? _creatureImage;

    public DesktopPetWindow()
    {
        InitializeComponent();
        _creatureImage = this.FindControl<Image>("CreatureImage");

        // 투명 창 설정 강화
        this.Background = Avalonia.Media.Brushes.Transparent;
        this.TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };

        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.RemoveRequested -= OnRemoveRequested;
        }

        _viewModel = DataContext as DesktopPetViewModel;

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.RemoveRequested += OnRemoveRequested;
            UpdatePosition();
            LoadCreatureImage();
        }
    }

    private static void Log(string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] [Image] {msg}";
        Console.WriteLine(line);
        try { System.IO.File.AppendAllText("pet_debug.log", line + "\n"); } catch { }
    }

    private void LoadCreatureImage()
    {
        if (_viewModel == null || _creatureImage == null) return;

        try
        {
            var spritePath = _viewModel.SpritePath;
            Log($"Loading sprite: {spritePath}");

            if (!string.IsNullOrEmpty(spritePath))
            {
                // 먼저 avares:// 시도
                try
                {
                    var uri = new Uri($"avares://TypingTamagotchi/Assets/{spritePath}");
                    _creatureImage.Source = new Bitmap(AssetLoader.Open(uri));
                    Log($"SUCCESS avares: {spritePath}");
                    return;
                }
                catch (Exception ex)
                {
                    Log($"avares failed: {ex.Message}");
                }

                // 실행 파일 기준 상대 경로로 시도
                var basePath = AppContext.BaseDirectory;
                var filePath = System.IO.Path.Combine(basePath, "Assets", spritePath);
                Log($"Trying file path: {filePath}");

                if (System.IO.File.Exists(filePath))
                {
                    _creatureImage.Source = new Bitmap(filePath);
                    Log($"SUCCESS file: {filePath}");
                    return;
                }

                Log($"File not found: {filePath}");
            }
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DesktopPetViewModel.X) ||
            e.PropertyName == nameof(DesktopPetViewModel.Y))
        {
            UpdatePosition();
        }
        else if (e.PropertyName == nameof(DesktopPetViewModel.IsFacingRight))
        {
            UpdateFlip();
        }
    }

    private void UpdatePosition()
    {
        if (_viewModel == null) return;
        Position = new PixelPoint((int)_viewModel.X, (int)_viewModel.Y);
    }

    private void UpdateFlip()
    {
        if (_viewModel == null || _creatureImage == null) return;

        var scaleX = _viewModel.IsFacingRight ? 1 : -1;
        _creatureImage.RenderTransform = new Avalonia.Media.TransformGroup
        {
            Children =
            {
                new Avalonia.Media.ScaleTransform(scaleX * _viewModel.AnimationScale, _viewModel.AnimationScale),
                new Avalonia.Media.RotateTransform(_viewModel.AnimationRotation)
            }
        };
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel == null) return;

        var point = e.GetCurrentPoint(this);

        if (point.Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _dragStart = e.GetPosition(this);
            _viewModel.OnDragStart();
            e.Pointer.Capture(this);
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging || _viewModel == null) return;

        var currentPos = e.GetPosition(this);
        var delta = currentPos - _dragStart;
        _dragStart = currentPos;  // 매 프레임마다 리셋

        var screens = Screens;
        var primaryScreen = screens.Primary;
        if (primaryScreen != null)
        {
            var bounds = primaryScreen.WorkingArea;
            _viewModel.OnDrag(delta.X, delta.Y, bounds.Width, bounds.Height);
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDragging || _viewModel == null) return;

        _isDragging = false;
        e.Pointer.Capture(null);

        var screens = Screens;
        var primaryScreen = screens.Primary;
        if (primaryScreen != null)
        {
            var bounds = primaryScreen.WorkingArea;
            _viewModel.OnDragEnd(bounds.Height);
        }
    }

    private void OnRemoveRequested()
    {
        _viewModel?.Cleanup();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _viewModel?.Cleanup();
    }
}
