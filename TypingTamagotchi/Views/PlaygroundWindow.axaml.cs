using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using TypingTamagotchi.ViewModels;

namespace TypingTamagotchi.Views;

public partial class PlaygroundWindow : Window
{
    private readonly PlaygroundViewModel _viewModel;

    public PlaygroundWindow()
    {
        InitializeComponent();
        _viewModel = new PlaygroundViewModel();
        DataContext = _viewModel;

        // 화면 하단(작업표시줄 위)에 위치시키기
        PositionAtBottom();
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
        _viewModel.Stop();
        base.OnClosed(e);
    }
}
