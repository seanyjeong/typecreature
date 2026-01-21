using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using TypingTamagotchi.Services;
using TypingTamagotchi.ViewModels;

namespace TypingTamagotchi.Views;

public partial class PlaygroundWindow : Window
{
    private readonly PlaygroundViewModel _viewModel;
    private readonly DatabaseService _db;

    public PlaygroundWindow()
    {
        InitializeComponent();
        _viewModel = new PlaygroundViewModel();
        _db = new DatabaseService();
        DataContext = _viewModel;

        // 저장된 크기 로드
        LoadSavedSize();

        // 화면 하단(작업표시줄 위)에 위치시키기
        PositionAtBottom();

        // 창 크기 변경 감지
        PropertyChanged += (s, e) =>
        {
            if (e.Property == WidthProperty && Width > 0)
            {
                _viewModel.PlaygroundWidth = Width;
            }
            else if (e.Property == HeightProperty && Height > 0)
            {
                _viewModel.PlaygroundHeight = Height;
            }
        };
    }

    private void LoadSavedSize()
    {
        var savedWidth = _db.GetSetting("playground_width");
        var savedHeight = _db.GetSetting("playground_height");

        if (double.TryParse(savedWidth, out var width) && width >= MinWidth)
        {
            Width = width;
            _viewModel.PlaygroundWidth = width;
        }
        if (double.TryParse(savedHeight, out var height) && height >= MinHeight)
        {
            Height = height;
            _viewModel.PlaygroundHeight = height;
        }
    }

    private void SaveSize()
    {
        _db.SaveSetting("playground_width", Width.ToString());
        _db.SaveSetting("playground_height", Height.ToString());
    }

    private void PositionAtBottom()
    {
        // 윈도우가 열릴 때 화면 크기를 가져와서 하단에 배치
        Opened += (_, _) =>
        {
            var screen = Screens.Primary;
            if (screen != null)
            {
                var workArea = screen.WorkingArea;
                var x = (workArea.Width - (int)Width) / 2 + workArea.X;
                var y = workArea.Height - (int)Height + workArea.Y - 10; // 약간의 여백
                Position = new PixelPoint(x, y);
            }
        };
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 드래그로 윈도우 이동
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel.Stop();
        Close();
    }

    private void OnRefreshClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel.Refresh();
    }

    private void OnResizePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control && control.Tag is string edge)
        {
            var windowEdge = edge switch
            {
                "BottomLeft" => WindowEdge.SouthWest,
                "BottomRight" => WindowEdge.SouthEast,
                "Bottom" => WindowEdge.South,
                "Left" => WindowEdge.West,
                "Right" => WindowEdge.East,
                _ => WindowEdge.South
            };

            BeginResizeDrag(windowEdge, e);
            e.Handled = true;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        // 크기 저장
        SaveSize();
        _viewModel.Stop();
        base.OnClosed(e);
    }
}
