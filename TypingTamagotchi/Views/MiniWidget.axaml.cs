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

        // 작업표시줄 아이콘 설정 (48번 크리처 - 황금드래곤)
        try
        {
            var iconUri = new Uri("avares://TypingTamagotchi/Assets/Creatures/48.png");
            using var stream = Avalonia.Platform.AssetLoader.Open(iconUri);
            Icon = new WindowIcon(stream);
        }
        catch { }

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
        Dispatcher.UIThread.Post(() => ShowHatchPopup(creature));
    }

    private void ShowHatchPopup(Creature creature)
    {
        // 등급별 색상
        var rarityColor = creature.Rarity switch
        {
            Rarity.Legendary => "#FFD700",
            Rarity.Epic => "#9C27B0",
            Rarity.Rare => "#2196F3",
            _ => "#4CAF50"
        };

        var rarityText = creature.Rarity switch
        {
            Rarity.Legendary => "★★★★ 전설",
            Rarity.Epic => "★★★ 영웅",
            Rarity.Rare => "★★ 희귀",
            _ => "★ 일반"
        };

        // 이미지 로드
        Bitmap? creatureImage = null;
        try
        {
            var uri = new Uri($"avares://TypingTamagotchi/Assets/{creature.SpritePath}");
            creatureImage = new Bitmap(Avalonia.Platform.AssetLoader.Open(uri));
        }
        catch { }

        // 팝업 창
        var popup = new Window
        {
            Width = 300,
            Height = 320,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            SystemDecorations = SystemDecorations.None,
            Background = Brushes.Transparent,
            Topmost = true,
            CanResize = false
        };

        // 메인 컨테이너
        var border = new Border
        {
            CornerRadius = new CornerRadius(16),
            BorderThickness = new Thickness(3),
            BorderBrush = new SolidColorBrush(Color.Parse(rarityColor)),
            Padding = new Thickness(20)
        };

        // 배경 그라데이션
        border.Background = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(Color.Parse("#2C3E50"), 0),
                new GradientStop(Color.Parse("#1A252F"), 1)
            }
        };

        var mainStack = new StackPanel { Spacing = 15 };

        // 타이틀
        mainStack.Children.Add(new TextBlock
        {
            Text = "🎉 부화 성공!",
            FontSize = 22,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        // 크리처 이미지
        if (creatureImage != null)
        {
            mainStack.Children.Add(new Image
            {
                Source = creatureImage,
                Width = 70,
                Height = 70,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }

        // 크리처 이름
        mainStack.Children.Add(new TextBlock
        {
            Text = creature.Name,
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse(rarityColor)),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        // 등급
        mainStack.Children.Add(new TextBlock
        {
            Text = rarityText,
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.Parse("#AAAAAA")),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        // 확인 버튼
        var confirmBtn = new Button
        {
            Content = "확인",
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(30, 10),
            Margin = new Thickness(0, 10, 0, 0),
            FontSize = 14,
            Background = new SolidColorBrush(Color.Parse(rarityColor)),
            Foreground = Brushes.Black,
            FontWeight = FontWeight.Bold
        };
        confirmBtn.Click += (s, e) => popup.Close();
        mainStack.Children.Add(confirmBtn);

        border.Child = mainStack;
        popup.Content = border;
        popup.Show();
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

        // 이미지 로드
        Bitmap? creatureImage = null;
        try
        {
            var uri = new Uri($"avares://TypingTamagotchi/Assets/{creature.SpritePath}");
            creatureImage = new Bitmap(Avalonia.Platform.AssetLoader.Open(uri));
        }
        catch { }

        var popup = new Window
        {
            Width = 340,
            Height = 420,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            SystemDecorations = SystemDecorations.None,
            Background = Brushes.Transparent,
            Topmost = true
        };

        var border = new Border
        {
            CornerRadius = new CornerRadius(16),
            BorderThickness = new Thickness(3),
            BorderBrush = new SolidColorBrush(Color.Parse(rarityColor)),
            Padding = new Thickness(20)
        };

        border.Background = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(Color.Parse("#2C3E50"), 0),
                new GradientStop(Color.Parse("#1A252F"), 1)
            }
        };

        var mainStack = new StackPanel { Spacing = 12 };

        // 상단: 이미지 + 기본정보
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
                Width = 80,
                Height = 80,
                Stretch = Stretch.Uniform
            });
        }

        var nameStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 4
        };

        nameStack.Children.Add(new TextBlock
        {
            Text = creature.Name,
            FontSize = 22,
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

        // 구분선
        mainStack.Children.Add(new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.Parse("#40FFFFFF")),
            Margin = new Thickness(0, 5, 0, 5)
        });

        // 상세 정보 그리드
        var infoGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto")
        };

        void AddInfoRow(int row, string label, string value)
        {
            var labelBlock = new TextBlock
            {
                Text = label,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.Parse("#888888")),
                Margin = new Thickness(0, 3, 15, 3)
            };
            Grid.SetRow(labelBlock, row);
            Grid.SetColumn(labelBlock, 0);
            infoGrid.Children.Add(labelBlock);

            var valueBlock = new TextBlock
            {
                Text = value,
                FontSize = 12,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 3, 0, 3)
            };
            Grid.SetRow(valueBlock, row);
            Grid.SetColumn(valueBlock, 1);
            infoGrid.Children.Add(valueBlock);
        }

        AddInfoRow(0, "나이:", creature.Age);
        AddInfoRow(1, "성별:", creature.Gender);
        AddInfoRow(2, "좋아하는 음식:", creature.FavoriteFood);
        AddInfoRow(3, "싫어하는 것:", creature.Dislikes);

        mainStack.Children.Add(infoGrid);

        // 성장배경 라벨
        mainStack.Children.Add(new TextBlock
        {
            Text = "성장배경",
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse(rarityColor)),
            Margin = new Thickness(0, 5, 0, 0)
        });

        // 성장배경 내용
        mainStack.Children.Add(new TextBlock
        {
            Text = creature.Background,
            FontSize = 12,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 18
        });

        // 닫기 버튼
        var closeBtn = new Button
        {
            Content = "닫기",
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(30, 8),
            Margin = new Thickness(0, 10, 0, 0),
            Background = new SolidColorBrush(Color.Parse(rarityColor)),
            Foreground = Brushes.Black,
            FontWeight = FontWeight.Bold
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

    private void OnShowcaseToggle(object? sender, PointerPressedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.IsShowcaseVisible = !_viewModel.IsShowcaseVisible;

            // 창 크기 조절 (아래쪽 고정, 위로 줄어들게)
            var oldHeight = Height;
            var newHeight = _viewModel.IsShowcaseVisible ? 500 : 140;
            var heightDiff = newHeight - oldHeight;

            Height = newHeight;
            Position = new PixelPoint(Position.X, Position.Y - (int)heightDiff);
        }
        e.Handled = true;
    }
}
