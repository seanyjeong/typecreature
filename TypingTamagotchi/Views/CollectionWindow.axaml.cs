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

                var closeBtn = new Button
                {
                    Content = "확인",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Padding = new Avalonia.Thickness(30, 8),
                    Background = new SolidColorBrush(Color.Parse("#4A6FA5")),
                    Foreground = Brushes.White,
                    FontWeight = FontWeight.Bold
                };
                closeBtn.Click += (s, args) => msgBox.Close();
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

            var popup = new Window
            {
                Title = creature.Name,
                Width = 400,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SystemDecorations = SystemDecorations.None,
                Background = Brushes.Transparent
            };

            var mainBorder = new Border
            {
                CornerRadius = new Avalonia.CornerRadius(16),
                Background = new LinearGradientBrush
                {
                    StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                    EndPoint = new Avalonia.RelativePoint(0, 1, Avalonia.RelativeUnit.Relative),
                    GradientStops = new GradientStops
                    {
                        new GradientStop(Color.Parse("#1A1A2E"), 0),
                        new GradientStop(Color.Parse("#16213E"), 1)
                    }
                },
                BorderBrush = new SolidColorBrush(Color.Parse("#4A5568")),
                BorderThickness = new Avalonia.Thickness(2),
                Padding = new Avalonia.Thickness(20)
            };

            var scroll = new ScrollViewer();
            var stack = new StackPanel { Spacing = 12 };

            // 크리처 이미지
            var imgBorder = new Border
            {
                Width = 150,
                Height = 150,
                Background = Brushes.White,
                CornerRadius = new Avalonia.CornerRadius(12),
                HorizontalAlignment = HorizontalAlignment.Center,
                ClipToBounds = true
            };

            try
            {
                var bitmap = new Bitmap(creature.SpritePath);
                imgBorder.Child = new Image
                {
                    Source = bitmap,
                    Stretch = Stretch.Uniform
                };
            }
            catch { }

            stack.Children.Add(imgBorder);

            // 이름 + 속성
            stack.Children.Add(new TextBlock
            {
                Text = $"{creature.ElementEmoji} {creature.Name}",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // 희귀도 + 속성
            var rarityColor = creature.Rarity switch
            {
                Models.Rarity.Common => "#81C784",
                Models.Rarity.Rare => "#64B5F6",
                Models.Rarity.Epic => "#BA68C8",
                Models.Rarity.Legendary => "#FFD54F",
                _ => "#FFFFFF"
            };
            stack.Children.Add(new TextBlock
            {
                Text = $"{creature.Rarity} • {creature.ElementName}",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.Parse(rarityColor)),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // 보유 개수
            stack.Children.Add(new TextBlock
            {
                Text = $"보유: {item.Count}마리",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.Parse("#888888")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            });

            // 설명
            if (!string.IsNullOrEmpty(creature.Description))
            {
                stack.Children.Add(CreateInfoSection("📖 설명", creature.Description));
            }

            // 상세 정보
            if (!string.IsNullOrEmpty(creature.Age))
                stack.Children.Add(CreateInfoRow("🎂 나이", creature.Age));
            if (!string.IsNullOrEmpty(creature.Gender))
                stack.Children.Add(CreateInfoRow("⚧ 성별", creature.Gender));
            if (!string.IsNullOrEmpty(creature.FavoriteFood))
                stack.Children.Add(CreateInfoRow("🍖 좋아하는 음식", creature.FavoriteFood));
            if (!string.IsNullOrEmpty(creature.Dislikes))
                stack.Children.Add(CreateInfoRow("💢 싫어하는 것", creature.Dislikes));

            // 배경 스토리
            if (!string.IsNullOrEmpty(creature.Background))
            {
                stack.Children.Add(CreateInfoSection("📜 배경", creature.Background));
            }

            // 닫기 버튼
            var closeBtn = new Button
            {
                Content = "닫기",
                HorizontalAlignment = HorizontalAlignment.Center,
                Padding = new Avalonia.Thickness(40, 10),
                Background = new SolidColorBrush(Color.Parse("#4A6FA5")),
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                Margin = new Avalonia.Thickness(0, 15, 0, 0)
            };
            closeBtn.Click += (s, args) => popup.Close();
            stack.Children.Add(closeBtn);

            scroll.Content = stack;
            mainBorder.Child = scroll;
            popup.Content = mainBorder;

            await popup.ShowDialog(this);
        }
    }

    private static Border CreateInfoSection(string title, string content)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#2C3E50")),
            CornerRadius = new Avalonia.CornerRadius(8),
            Padding = new Avalonia.Thickness(12)
        };

        var stack = new StackPanel { Spacing = 5 };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.Parse("#888888"))
        });
        stack.Children.Add(new TextBlock
        {
            Text = content,
            FontSize = 13,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap
        });

        border.Child = stack;
        return border;
    }

    private static Border CreateInfoRow(string label, string value)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#2C3E50")),
            CornerRadius = new Avalonia.CornerRadius(6),
            Padding = new Avalonia.Thickness(10, 6)
        };

        var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("Auto,*") };
        grid.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.Parse("#888888")),
            Margin = new Avalonia.Thickness(0, 0, 10, 0)
        });

        var valueText = new TextBlock
        {
            Text = value,
            FontSize = 12,
            Foreground = Brushes.White
        };
        Grid.SetColumn(valueText, 1);
        grid.Children.Add(valueText);

        border.Child = grid;
        return border;
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

                var closeBtn = new Button
                {
                    Content = "확인",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Padding = new Avalonia.Thickness(30, 8),
                    Background = new SolidColorBrush(Color.Parse("#90EE90")),
                    Foreground = Brushes.Black,
                    FontWeight = FontWeight.Bold
                };
                closeBtn.Click += (s, args) => msgBox.Close();
                stack.Children.Add(closeBtn);

                border.Child = stack;
                msgBox.Content = border;
                await msgBox.ShowDialog(this);
            }
        }
    }
}
