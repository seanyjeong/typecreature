using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace TypingTamagotchi.Views;

public partial class MiniWidget : Window
{
    private bool _isDragging;
    private Point _dragStartPoint;

    public MiniWidget()
    {
        InitializeComponent();

        // 드래그 이동 지원
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;

        // 화면 오른쪽 하단에 위치
        Opened += (s, e) =>
        {
            var screen = Screens.Primary;
            if (screen != null)
            {
                var workArea = screen.WorkingArea;
                Position = new PixelPoint(
                    workArea.Right - (int)Width - 20,
                    workArea.Bottom - (int)Height - 20
                );
            }
        };
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _dragStartPoint = e.GetPosition(this);
            e.Pointer.Capture(this);
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging)
        {
            var currentPoint = e.GetPosition(this);
            var offset = currentPoint - _dragStartPoint;
            Position = new PixelPoint(
                Position.X + (int)offset.X,
                Position.Y + (int)offset.Y
            );
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
        e.Pointer.Capture(null);
    }
}
