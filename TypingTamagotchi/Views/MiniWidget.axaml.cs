using System;
using System.Linq;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using TypingTamagotchi.Models;
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
    private bool _hasMoved; // 움직임 있었는지 추적
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

        // 진열장 변경 이벤트 구독 (실시간 반영)
        CollectionViewModel.DisplayChanged += OnDisplayChanged;

        Closed += (s, e) =>
        {
            CollectionViewModel.DisplayChanged -= OnDisplayChanged;
        };
    }

    private void OnDisplayChanged()
    {
        Dispatcher.UIThread.Post(() => _viewModel?.Refresh());
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _viewModel = DataContext as MiniWidgetViewModel;

        if (_viewModel != null)
        {
            _viewModel.CreatureHatched += OnCreatureHatched;
        }
    }

    private void OnCreatureHatched(Creature creature)
    {
        Dispatcher.UIThread.Post(() => ShowHatchToast(creature));
    }

    private void ShowHatchToast(Creature creature)
    {
        // 토스트 창 만들기
        var toast = new Window
        {
            Width = 250,
            Height = 100,
            WindowStartupLocation = WindowStartupLocation.Manual,
            SystemDecorations = SystemDecorations.None,
            Topmost = true,
            Background = Brushes.Transparent,
            CanResize = false
        };

        // 화면 오른쪽 하단에 위치
        var screen = Screens.Primary;
        if (screen != null)
        {
            var workArea = screen.WorkingArea;
            toast.Position = new PixelPoint(
                workArea.Right - 270,
                workArea.Bottom - 130
            );
        }

        // 등급별 색상
        var rarityColor = creature.Rarity switch
        {
            Rarity.Legendary => "#FFD700",
            Rarity.Epic => "#9C27B0",
            Rarity.Rare => "#2196F3",
            _ => "#4CAF50"
        };

        // 이미지 로드 (avares:// 사용)
        Bitmap? creatureImage = null;
        try
        {
            var uri = new Uri($"avares://TypingTamagotchi/Assets/{creature.SpritePath}");
            creatureImage = new Bitmap(Avalonia.Platform.AssetLoader.Open(uri));
        }
        catch { }

        // 토스트 내용
        var border = new Border
        {
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(2),
            BorderBrush = new SolidColorBrush(Color.Parse(rarityColor)),
            Background = new SolidColorBrush(Color.Parse("#E0303050")),
            Padding = new Thickness(15)
        };

        var stack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 15
        };

        if (creatureImage != null)
        {
            stack.Children.Add(new Image
            {
                Source = creatureImage,
                Width = 60,
                Height = 60,
                Stretch = Stretch.Uniform
            });
        }

        var textStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 5
        };

        textStack.Children.Add(new TextBlock
        {
            Text = "🎉 부화 성공!",
            FontSize = 14,
            Foreground = Brushes.White
        });

        textStack.Children.Add(new TextBlock
        {
            Text = creature.Name,
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse(rarityColor))
        });

        textStack.Children.Add(new TextBlock
        {
            Text = creature.Rarity.ToString(),
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#AAAAAA"))
        });

        stack.Children.Add(textStack);
        border.Child = stack;
        toast.Content = border;

        toast.Show();

        // 3초 후 자동으로 닫기
        var timer = new Timer(3000);
        timer.Elapsed += (s, e) =>
        {
            timer.Stop();
            Dispatcher.UIThread.Post(() => toast.Close());
        };
        timer.Start();
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
                _hasMoved = false; // 초기화
                _isSlotDragging = false; // 초기화
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
        if (_dragSourceIndex >= 0 && !_isWindowDragging)
        {
            var delta = currentPos - _slotDragStart;
            if (Math.Abs(delta.X) > 2 || Math.Abs(delta.Y) > 2)
            {
                _hasMoved = true; // 움직임 감지
            }
            if (!_isSlotDragging && (Math.Abs(delta.X) > DragThreshold || Math.Abs(delta.Y) > DragThreshold))
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
            // 드롭 대상 슬롯 찾기 (위치 기반 히트 테스트)
            var pos = e.GetPosition(this);
            var targetSlot = FindSlotAtPosition(pos);

            if (targetSlot != null && targetSlot.SlotIndex != _dragSourceIndex)
            {
                _viewModel.SwapSlots(_dragSourceIndex, targetSlot.SlotIndex);
            }

            // 드래그 상태 복원
            if (_dragSourceBorder != null)
            {
                _dragSourceBorder.Opacity = 1.0;
            }
            ClearAllHighlights();
        }
        else if (!_hasMoved && !_isWindowDragging && _dragSourceIndex >= 0)
        {
            // 움직임 없이 클릭만 한 경우 - 크리처 정보 팝업
            var slot = _viewModel?.DisplaySlots[_dragSourceIndex];
            if (slot?.HasCreature == true)
            {
                ShowCreatureInfo(slot.GetCreature()!);
            }
        }

        // 상태 초기화
        _isWindowDragging = false;
        _isSlotDragging = false;
        _hasMoved = false;
        _dragSourceIndex = -1;
        _dragSourceBorder = null;
        e.Pointer.Capture(null);
    }

    private DisplaySlot? FindSlotAtPosition(Point pos)
    {
        // 모든 슬롯 Border를 찾아서 위치 확인
        var slotBorders = this.GetVisualDescendants()
            .OfType<Border>()
            .Where(b => b.Classes.Contains("slot"));

        foreach (var border in slotBorders)
        {
            var slotPos = border.TranslatePoint(new Point(0, 0), this);
            if (slotPos.HasValue)
            {
                var rect = new Rect(slotPos.Value, border.Bounds.Size);
                if (rect.Contains(pos))
                {
                    return border.DataContext as DisplaySlot;
                }
            }
        }
        return null;
    }

    private void ShowCreatureInfo(Creature creature)
    {
        // 등급별 색상
        var rarityColor = creature.Rarity switch
        {
            Rarity.Legendary => "#FFD700",
            Rarity.Epic => "#9C27B0",
            Rarity.Rare => "#2196F3",
            _ => "#4CAF50"
        };

        // 이미지 로드 (avares:// 사용)
        Bitmap? creatureImage = null;
        try
        {
            var uri = new Uri($"avares://TypingTamagotchi/Assets/{creature.SpritePath}");
            creatureImage = new Bitmap(Avalonia.Platform.AssetLoader.Open(uri));
        }
        catch { }

        var popup = new Window
        {
            Width = 280,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            SystemDecorations = SystemDecorations.None,
            Background = Brushes.Transparent,
            Topmost = true
        };

        var border = new Border
        {
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(2),
            BorderBrush = new SolidColorBrush(Color.Parse(rarityColor)),
            Background = new SolidColorBrush(Color.Parse("#F0202030")),
            Padding = new Thickness(15)
        };

        var mainStack = new StackPanel { Spacing = 10 };

        // 상단: 이미지 + 이름
        var headerStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 15,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        if (creatureImage != null)
        {
            headerStack.Children.Add(new Image
            {
                Source = creatureImage,
                Width = 70,
                Height = 70,
                Stretch = Stretch.Uniform
            });
        }

        var nameStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 5
        };

        nameStack.Children.Add(new TextBlock
        {
            Text = creature.Name,
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse(rarityColor))
        });

        nameStack.Children.Add(new TextBlock
        {
            Text = creature.Rarity switch
            {
                Rarity.Legendary => "★★★★ 전설",
                Rarity.Epic => "★★★ 영웅",
                Rarity.Rare => "★★ 희귀",
                _ => "★ 일반"
            },
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.Parse("#AAAAAA"))
        });

        headerStack.Children.Add(nameStack);
        mainStack.Children.Add(headerStack);

        // 설명
        mainStack.Children.Add(new TextBlock
        {
            Text = creature.Description,
            FontSize = 14,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        // 닫기 버튼
        var closeBtn = new Button
        {
            Content = "닫기",
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(20, 5)
        };
        closeBtn.Click += (s, e) => popup.Close();
        mainStack.Children.Add(closeBtn);

        border.Child = mainStack;
        popup.Content = border;
        popup.Show();
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

    private void OnCollectionTitleClick(object? sender, PointerPressedEventArgs e)
    {
        // 도감 열기
        var collectionWindow = new CollectionWindow
        {
            DataContext = new CollectionViewModel()
        };

        collectionWindow.Closed += (s, args) =>
        {
            _viewModel?.Refresh();
        };

        collectionWindow.Show();
        e.Handled = true; // 창 드래그 방지
    }
}
