using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AutoMerge.UI.Views.Panels;

public sealed partial class AiChatPanelView : UserControl
{
    public AiChatPanelView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
