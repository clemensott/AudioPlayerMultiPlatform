using System.Threading.Tasks;
using AudioPlayerBackend.Build;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AudioPlayerFrontendAvalonia.Views;

public partial class BuildOpenWindow : Window
{
    private AudioServicesBuilder builder;

    public AudioServicesBuilder Builder
    {
        get => builder;
        set => DataContext = builder = value;
    }

    public BuildOpenWindow(AudioServicesBuilder builder)
    {
        InitializeComponent();

        this.builder = builder;
        Builder = builder;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!e.IsProgrammatic) Builder.Cancel();
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        await AwaitBuild();
    }

    private async Task AwaitBuild()
    {
        await Task.Delay(100);
        BuildEndedType result = await Builder.CompleteToken.EndTask;

        if (result is BuildEndedType.Successful or BuildEndedType.Settings)
        {
            Close();
        }
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e)
    {
        Builder.Cancel();
        Close();
    }

    private void BtnOpeningSettings_Click(object? sender, RoutedEventArgs e)
    {
        Builder.Settings();
    }

    private async void BtnException_Click(object? sender, RoutedEventArgs e)
    {
        string? message = Builder.CompleteToken.Exception?.Message;
        if (message is not null) await DialogWindow.ShowPrimary(this, message, "Build Exception");
    }
}