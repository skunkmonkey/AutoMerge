using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AutoMerge.UI.Views.Panels;

public sealed partial class MergedResultView : UserControl
{
    public MergedResultView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
