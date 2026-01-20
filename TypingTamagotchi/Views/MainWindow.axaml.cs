using Avalonia.Controls;
using TypingTamagotchi.ViewModels;

namespace TypingTamagotchi.Views;

public partial class MainWindow : Window
{
    private MiniWidget? _miniWidget;

    public MainWindow()
    {
        InitializeComponent();

        // DataContext가 설정된 후 이벤트 연결
        DataContextChanged += (s, e) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.OpenCollectionRequested += OnOpenCollectionRequested;
                vm.ToggleWidgetRequested += OnToggleWidgetRequested;
            }
        };

        // 메인 창 닫힐 때 위젯도 닫기
        Closed += (s, e) =>
        {
            _miniWidget?.Close();
        };
    }

    private void OnOpenCollectionRequested()
    {
        var collectionWindow = new CollectionWindow();
        collectionWindow.ShowDialog(this);
    }

    private void OnToggleWidgetRequested(bool show)
    {
        if (show)
        {
            if (_miniWidget == null || !_miniWidget.IsVisible)
            {
                _miniWidget = new MiniWidget
                {
                    DataContext = this.DataContext
                };
                _miniWidget.Show();
            }
        }
        else
        {
            _miniWidget?.Close();
            _miniWidget = null;
        }
    }
}
