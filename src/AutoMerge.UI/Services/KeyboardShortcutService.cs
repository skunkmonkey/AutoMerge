using Avalonia.Controls;
using Avalonia.Input;

namespace AutoMerge.UI.Services;

public sealed class KeyboardShortcutService
{
    public void Register(
        Window window,
        Action onAccept,
        Action onCancel,
        Action onSaveDraft,
        Action onUndo,
        Action onRedo,
        Action onOpenPreferences)
    {
        window.KeyDown += (_, args) =>
        {
            var hasPrimaryModifier = args.KeyModifiers.HasFlag(KeyModifiers.Control) || args.KeyModifiers.HasFlag(KeyModifiers.Meta);
            if (hasPrimaryModifier)
            {
                if (args.Key == Key.Enter)
                {
                    onAccept();
                    args.Handled = true;
                    return;
                }

                if (args.Key == Key.S)
                {
                    onSaveDraft();
                    args.Handled = true;
                    return;
                }

                if (args.Key == Key.Z)
                {
                    onUndo();
                    args.Handled = true;
                    return;
                }

                if (args.Key == Key.Y)
                {
                    onRedo();
                    args.Handled = true;
                    return;
                }

                if (args.Key == Key.OemComma)
                {
                    onOpenPreferences();
                    args.Handled = true;
                    return;
                }
            }

            if (args.Key == Key.Escape)
            {
                onCancel();
                args.Handled = true;
            }
        };
    }
}
