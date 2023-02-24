using CommunityToolkit.Mvvm.ComponentModel;

namespace Furnace.Runnable;

public class ObservableProgress : ObservableObject
{
    private string _tag;
    private double _progress;

    public string Tag
    {
        get => _tag;
        set => SetProperty(ref _tag, value);
    }
    
    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }
}