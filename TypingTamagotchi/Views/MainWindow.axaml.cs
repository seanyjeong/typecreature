using Avalonia.Controls;
using TypingTamagotchi.ViewModels;

namespace TypingTamagotchi.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // DataContext가 설정된 후 이벤트 연결
        DataContextChanged += (s, e) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.OpenCollectionRequested += OnOpenCollectionRequested;
            }
        };
    }

    private void OnOpenCollectionRequested()
    {
        var collectionWindow = new CollectionWindow();
        collectionWindow.ShowDialog(this);
    }
}
