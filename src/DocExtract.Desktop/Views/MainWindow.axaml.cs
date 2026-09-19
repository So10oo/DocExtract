using Avalonia.Controls;
using DocExtract.Desktop.ViewModels;

namespace DocExtract.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.Host = this;
        }

        if (this.FindControl<PageViewer>("Viewer") is { } viewer)
        {
            viewer.SelectionCompleted += async (_, box) =>
            {
                if (DataContext is MainViewModel inner)
                {
                    await inner.OnSelectionCompletedAsync(box);
                }
            };
        }
    }
}
