using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using TypingTamagotchi.ViewModels;

namespace TypingTamagotchi.Views;

public partial class MiniWidget : Window
{
    private MiniWidgetViewModel? _viewModel;
    private bool _isWindowDragging;
    private Point _windowDragStart;

    // 슬롯 드래그 관련
    private bool _isSlotDragging;
    private int _dragSourceIndex = -1;
    private Border? _dragSourceBorder;
    private Point _slotDragStart;
    private const double DragThreshold = 5.0;

    public MiniWidget()
    {
        InitializeComponent();

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

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _viewModel = DataContext as MiniWidgetViewModel;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var position = e.GetPosition(this);

        // 슬롯 위에서 시작했는지 확인
        var hitBorder = FindSlotBorderAt(e.Source as Visual);
        if (hitBorder != null && _viewModel != null)
        {
            var slot = hitBorder.DataContext as DisplaySlot;
            if (slot != null && slot.HasCreature)
            {
                _slotDragStart = position;
                _dragSourceBorder = hitBorder;
                _dragSourceIndex = slot.SlotIndex;
                e.Pointer.Capture(this);
                return;
            }
        }

        // 창 드래그 시작
        _isWindowDragging = true;
        _windowDragStart = position;
        e.Pointer.Capture(this);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var currentPos = e.GetPosition(this);

        // 슬롯 드래그 감지
        if (_dragSourceIndex >= 0 && !_isSlotDragging && !_isWindowDragging)
        {
            var delta = currentPos - _slotDragStart;
            if (Math.Abs(delta.X) > DragThreshold || Math.Abs(delta.Y) > DragThreshold)
            {
                _isSlotDragging = true;
                if (_dragSourceBorder != null)
                {
                    _dragSourceBorder.Opacity = 0.5;
                }
            }
        }

        // 슬롯 드래그 중
        if (_isSlotDragging)
        {
            // 현재 마우스 위치의 슬롯 하이라이트
            UpdateDragHighlight(e.Source as Visual);
            return;
        }

        // 창 드래그
        if (_isWindowDragging)
        {
            var offset = currentPos - _windowDragStart;
            Position = new PixelPoint(
                Position.X + (int)offset.X,
                Position.Y + (int)offset.Y
            );
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isSlotDragging && _dragSourceIndex >= 0 && _viewModel != null)
        {
            // 드롭 대상 슬롯 찾기
            var targetBorder = FindSlotBorderAt(e.Source as Visual);
            if (targetBorder != null)
            {
                var targetSlot = targetBorder.DataContext as DisplaySlot;
                if (targetSlot != null && targetSlot.SlotIndex != _dragSourceIndex)
                {
                    _viewModel.SwapSlots(_dragSourceIndex, targetSlot.SlotIndex);
                }
            }

            // 드래그 상태 복원
            if (_dragSourceBorder != null)
            {
                _dragSourceBorder.Opacity = 1.0;
            }
            ClearAllHighlights();
        }

        // 상태 초기화
        _isWindowDragging = false;
        _isSlotDragging = false;
        _dragSourceIndex = -1;
        _dragSourceBorder = null;
        e.Pointer.Capture(null);
    }

    private Border? FindSlotBorderAt(Visual? visual)
    {
        while (visual != null)
        {
            if (visual is Border border && border.Classes.Contains("slot"))
            {
                return border;
            }
            visual = visual.GetVisualParent();
        }
        return null;
    }

    private void UpdateDragHighlight(Visual? visual)
    {
        ClearAllHighlights();

        var targetBorder = FindSlotBorderAt(visual);
        if (targetBorder != null && targetBorder != _dragSourceBorder)
        {
            var slot = targetBorder.DataContext as DisplaySlot;
            if (slot != null)
            {
                slot.IsDragOver = true;
            }
        }
    }

    private void ClearAllHighlights()
    {
        if (_viewModel == null) return;

        foreach (var slot in _viewModel.DisplaySlots)
        {
            slot.IsDragOver = false;
        }
    }

    public void RefreshDisplay()
    {
        _viewModel?.Refresh();
    }

    public void OnKeyboardInput()
    {
        _viewModel?.OnInput();
    }
}
