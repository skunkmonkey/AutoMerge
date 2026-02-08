using AutoMerge.Core.Models;
using AutoMerge.UI.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoMerge.UI.ViewModels;

public sealed partial class MergeInputDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string? _basePath;

    [ObservableProperty]
    private string? _localPath;

    [ObservableProperty]
    private string? _remotePath;

    [ObservableProperty]
    private string? _mergedPath;

    [ObservableProperty]
    private string? _errorMessage;

    public bool CanConfirm => !string.IsNullOrWhiteSpace(LocalPath) &&
                              !string.IsNullOrWhiteSpace(RemotePath) &&
                              !string.IsNullOrWhiteSpace(MergedPath);

    public bool TryBuildMergeInput(out MergeInput? mergeInput, out string? errorMessage)
    {
        if (!CanConfirm)
        {
            mergeInput = null;
            errorMessage = UIStrings.MergeInputSelectPathsError;
            return false;
        }

        var basePath = string.IsNullOrWhiteSpace(BasePath) ? LocalPath! : BasePath;

        try
        {
            mergeInput = new MergeInput(basePath, LocalPath!, RemotePath!, MergedPath!);
            errorMessage = null;
            return true;
        }
        catch (ArgumentException ex)
        {
            mergeInput = null;
            errorMessage = ex.Message;
            return false;
        }
    }

    partial void OnBasePathChanged(string? value)
    {
        OnPathChanged();
    }

    partial void OnLocalPathChanged(string? value)
    {
        OnPathChanged();
    }

    partial void OnRemotePathChanged(string? value)
    {
        OnPathChanged();
    }

    partial void OnMergedPathChanged(string? value)
    {
        OnPathChanged();
    }

    private void OnPathChanged()
    {
        ErrorMessage = null;
        OnPropertyChanged(nameof(CanConfirm));
    }
}
