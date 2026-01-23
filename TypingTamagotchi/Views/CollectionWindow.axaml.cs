using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using TypingTamagotchi.ViewModels;

namespace TypingTamagotchi.Views;

public partial class CollectionWindow : Window
{
    public CollectionWindow()
    {
        InitializeComponent();
        // DataContext is set by App.axaml.cs
    }

    private async void OnToggleDisplayClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is CollectionItem item)
        {
            if (DataContext is CollectionViewModel vm)
            {
                var wasInDisplay = item.IsInDisplay;
                vm.ToggleDisplayCommand.Execute(item);

                // 결과 메시지 표시
                var message = wasInDisplay
                    ? $"'{item.Creature.Name}'을(를) 진열장에서 제거했습니다."
                    : item.IsInDisplay
                        ? $"'{item.Creature.Name}'을(를) 진열장에 추가했습니다!"
                        : "진열장이 가득 찼습니다!";

                var msgBox = new Window
                {
                    Title = "알림",
                    Width = 320,
                    Height = 140,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    SystemDecorations = SystemDecorations.None,
                    Background = Brushes.Transparent
                };

                var border = new Border
                {
                    CornerRadius = new Avalonia.CornerRadius(12),
                    Padding = new Avalonia.Thickness(20),
                    Background = new LinearGradientBrush
                    {
                        StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                        EndPoint = new Avalonia.RelativePoint(0, 1, Avalonia.RelativeUnit.Relative),
                        GradientStops = new GradientStops
                        {
                            new GradientStop(Color.Parse("#2C3E50"), 0),
                            new GradientStop(Color.Parse("#1A252F"), 1)
                        }
                    },
                    BorderBrush = new SolidColorBrush(Color.Parse("#4A5568")),
                    BorderThickness = new Avalonia.Thickness(2)
                };

                var stack = new StackPanel { Spacing = 15 };

                stack.Children.Add(new TextBlock
                {
                    Text = message,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 14,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center
                });

                var displayBaseBrush = new SolidColorBrush(Color.Parse("#4A6FA5"));
                var displayHoverBrush = new SolidColorBrush(Color.Parse("#6A8FC5"));
                var displayPressedBrush = new SolidColorBrush(Color.Parse("#3A5F85"));

                var closeBtn = new Button
                {
                    Content = "확인",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Padding = new Avalonia.Thickness(30, 8),
                    Background = displayBaseBrush,
                    Foreground = Brushes.White,
                    FontWeight = FontWeight.Bold
                };
                closeBtn.Click += (s, args) => msgBox.Close();
                closeBtn.PointerEntered += (s, args) => closeBtn.Background = displayHoverBrush;
                closeBtn.PointerExited += (s, args) => closeBtn.Background = displayBaseBrush;
                closeBtn.PointerPressed += (s, args) => closeBtn.Background = displayPressedBrush;
                closeBtn.PointerReleased += (s, args) => closeBtn.Background = displayBaseBrush;
                stack.Children.Add(closeBtn);

                border.Child = stack;
                msgBox.Content = border;
                await msgBox.ShowDialog(this);
            }
        }
    }

    private async void OnCreatureClick(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is CollectionItem item && item.IsOwned)
        {
            var creature = item.Creature;

            // 등급별 색상
            var rarityColor = creature.Rarity switch
            {
                Models.Rarity.Legendary => "#FFD700",
                Models.Rarity.Epic => "#9C27B0",
                Models.Rarity.Rare => "#2196F3",
                _ => "#4CAF50"
            };

            var rarityText = creature.Rarity switch
            {
                Models.Rarity.Legendary => "★★★★ 전설",
                Models.Rarity.Epic => "★★★ 영웅",
                Models.Rarity.Rare => "★★ 희귀",
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
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SystemDecorations = SystemDecorations.None,
                Background = Brushes.Transparent,
                CanResize = false
            };

            // 메인 컨테이너
            var mainBorder = new Border
            {
                CornerRadius = new Avalonia.CornerRadius(16),
                BorderThickness = new Avalonia.Thickness(3),
                BorderBrush = new SolidColorBrush(Color.Parse(rarityColor)),
                Padding = new Avalonia.Thickness(20)
            };

            // 배경 그라데이션
            mainBorder.Background = new LinearGradientBrush
            {
                StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                EndPoint = new Avalonia.RelativePoint(0, 1, Avalonia.RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.Parse("#2C3E50"), 0),
                    new GradientStop(Color.Parse("#1A252F"), 1)
                }
            };

            var mainStack = new StackPanel { Spacing = 12 };

            // 크리처 이미지
            if (creatureImage != null)
            {
                mainStack.Children.Add(new Image
                {
                    Source = creatureImage,
                    Width = 100,
                    Height = 100,
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

            // 속성 + 등급
            var infoStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 10
            };

            infoStack.Children.Add(new TextBlock
            {
                Text = $"{creature.ElementEmoji} {creature.ElementName}",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.Parse("#AAAAAA")),
                VerticalAlignment = VerticalAlignment.Center
            });

            infoStack.Children.Add(new TextBlock
            {
                Text = "|",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.Parse("#555555")),
                VerticalAlignment = VerticalAlignment.Center
            });

            infoStack.Children.Add(new TextBlock
            {
                Text = rarityText,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.Parse("#AAAAAA")),
                VerticalAlignment = VerticalAlignment.Center
            });

            mainStack.Children.Add(infoStack);

            // 보유 개수
            mainStack.Children.Add(new TextBlock
            {
                Text = $"보유: {item.Count}마리",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.Parse("#888888")),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // 닫기 버튼 - 등급별 hover/pressed 색상
            var (hoverColor, pressedColor) = rarityColor switch
            {
                "#FFD700" => ("#FFEA00", "#DAA520"),  // Legendary
                "#9C27B0" => ("#BA68C8", "#7B1FA2"),  // Epic
                "#2196F3" => ("#64B5F6", "#1976D2"),  // Rare
                _ => ("#81C784", "#388E3C")           // Common
            };
            var baseBrush = new SolidColorBrush(Color.Parse(rarityColor));
            var hoverBrush = new SolidColorBrush(Color.Parse(hoverColor));
            var pressedBrush = new SolidColorBrush(Color.Parse(pressedColor));

            var closeBtn = new Button
            {
                Content = "확인",
                HorizontalAlignment = HorizontalAlignment.Center,
                Padding = new Avalonia.Thickness(40, 10),
                Margin = new Avalonia.Thickness(0, 10, 0, 0),
                FontSize = 14,
                Background = baseBrush,
                Foreground = Brushes.Black,
                FontWeight = FontWeight.Bold
            };
            closeBtn.Click += (s, args) => popup.Close();
            closeBtn.PointerEntered += (s, args) => closeBtn.Background = hoverBrush;
            closeBtn.PointerExited += (s, args) => closeBtn.Background = baseBrush;
            closeBtn.PointerPressed += (s, args) => closeBtn.Background = pressedBrush;
            closeBtn.PointerReleased += (s, args) => closeBtn.Background = baseBrush;
            mainStack.Children.Add(closeBtn);

            mainBorder.Child = mainStack;
            popup.Content = mainBorder;

            await popup.ShowDialog(this);
        }
    }

    private async void OnTogglePlaygroundClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is CollectionItem item)
        {
            if (DataContext is CollectionViewModel vm)
            {
                var wasInPlayground = item.IsInPlayground;
                vm.TogglePlaygroundCommand.Execute(item);

                // 결과 메시지 표시
                var message = wasInPlayground
                    ? $"'{item.Creature.Name}'을(를) 놀이터에서 데려왔습니다."
                    : item.IsInPlayground
                        ? $"'{item.Creature.Name}'을(를) 놀이터로 보냈습니다!"
                        : "놀이터가 가득 찼습니다! (최대 6마리)";

                var msgBox = new Window
                {
                    Title = "알림",
                    Width = 320,
                    Height = 140,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    SystemDecorations = SystemDecorations.None,
                    Background = Brushes.Transparent
                };

                var border = new Border
                {
                    CornerRadius = new Avalonia.CornerRadius(12),
                    Padding = new Avalonia.Thickness(20),
                    Background = new LinearGradientBrush
                    {
                        StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                        EndPoint = new Avalonia.RelativePoint(0, 1, Avalonia.RelativeUnit.Relative),
                        GradientStops = new GradientStops
                        {
                            new GradientStop(Color.Parse("#2C3E50"), 0),
                            new GradientStop(Color.Parse("#1A252F"), 1)
                        }
                    },
                    BorderBrush = new SolidColorBrush(Color.Parse("#4A5568")),
                    BorderThickness = new Avalonia.Thickness(2)
                };

                var stack = new StackPanel { Spacing = 15 };

                stack.Children.Add(new TextBlock
                {
                    Text = message,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 14,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center
                });

                var playBaseBrush = new SolidColorBrush(Color.Parse("#90EE90"));
                var playHoverBrush = new SolidColorBrush(Color.Parse("#B0FFB0"));
                var playPressedBrush = new SolidColorBrush(Color.Parse("#60BE60"));

                var closeBtn = new Button
                {
                    Content = "확인",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Padding = new Avalonia.Thickness(30, 8),
                    Background = playBaseBrush,
                    Foreground = Brushes.Black,
                    FontWeight = FontWeight.Bold
                };
                closeBtn.Click += (s, args) => msgBox.Close();
                closeBtn.PointerEntered += (s, args) => closeBtn.Background = playHoverBrush;
                closeBtn.PointerExited += (s, args) => closeBtn.Background = playBaseBrush;
                closeBtn.PointerPressed += (s, args) => closeBtn.Background = playPressedBrush;
                closeBtn.PointerReleased += (s, args) => closeBtn.Background = playBaseBrush;
                stack.Children.Add(closeBtn);

                border.Child = stack;
                msgBox.Content = border;
                await msgBox.ShowDialog(this);
            }
        }
    }
}
