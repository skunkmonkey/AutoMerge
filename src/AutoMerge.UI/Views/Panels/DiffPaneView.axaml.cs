using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AutoMerge.UI.Views.Panels;

public sealed partial class DiffPaneView : UserControl
{
    public DiffPaneView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
