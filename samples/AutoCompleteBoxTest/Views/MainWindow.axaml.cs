using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
namespace AutoCompleteBoxTest.Views;

public partial class MainWindow : Window {
    public MainWindow() {
        this.InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        
        #if DEBUG
            this.AttachDevTools();
        #endif
    }
}
