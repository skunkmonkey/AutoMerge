using Avalonia.Controls;

namespace AutoMerge.UI.Services;

public sealed class DialogService
{
    public Task<TResult?> ShowDialogAsync<TResult>(Window owner, Window dialog)
    {
        return dialog.ShowDialog<TResult?>(owner);
    }

    public Task ShowDialogAsync(Window owner, Window dialog)
    {
        return dialog.ShowDialog(owner);
    }
}
