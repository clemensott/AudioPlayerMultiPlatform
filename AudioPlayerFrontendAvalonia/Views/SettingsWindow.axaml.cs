using System.Linq;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.Build;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using StdOttStandard.Linq;

namespace AudioPlayerFrontendAvalonia.Views;

public partial class SettingsWindow : Window
{
    public AudioServicesBuildConfig ServiceConfig { get; private set; }

    public SettingsWindow(AudioServicesBuildConfig serviceConfig)
    {
        InitializeComponent();

        DataContext = ServiceConfig = serviceConfig;

        if (serviceConfig.BuildServer) tbxPort.Text = serviceConfig.ServerPort.ToString();
        else if (serviceConfig.BuildClient) tbxPort.Text = serviceConfig.ClientPort?.ToString();

        lbxDefaultUpdateRoots.ItemsSource = serviceConfig.DefaultUpdateRoots;
    }

    private void RbnStandalone_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        ServiceConfig.WithStandalone();
    }

    private void RbnServer_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        ServiceConfig.WithServer(ServiceConfig.ServerPort);
        tbxPort.Text = ServiceConfig.ServerPort.ToString();
    }

    private void RbnClient_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        ServiceConfig.WithClient(tbxServerAddress.Text, ServiceConfig.ClientPort);
        tbxPort.Text = ServiceConfig.ClientPort?.ToString();
    }

    private void TbxPort_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (ServiceConfig.BuildServer)
        {
            if (int.TryParse(tbxPort.Text, out int port)) ServiceConfig.ServerPort = port;
        }
        else if (ServiceConfig.BuildClient)
        {
            if (string.IsNullOrWhiteSpace(tbxPort.Text)) ServiceConfig.ClientPort = null;
            else if (int.TryParse(tbxPort.Text, out int port)) ServiceConfig.ClientPort = port;
        }
    }

    private void LbxDefaultUpdateRoots_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        lbxDefaultUpdateRoots.SelectedIndex = -1;
    }

    private void DefaultUpdateRootControl_ValueChanged(object? sender, FileMediaSourceRootInfo e)
    {
        int index = lbxDefaultUpdateRoots.ItemsSource.ToNotNull().IndexOf(((StyledElement)sender!).DataContext);
        if (index < 0) return;

        FileMediaSourceRootInfo[] newDefaultRoots = ServiceConfig.DefaultUpdateRoots.ToArray();
        newDefaultRoots[index] = e;
        ServiceConfig.WithDefaultUpdateRoots(newDefaultRoots);
    }

    private void BtnAddDefaultUpdateRoot_Click(object? sender, RoutedEventArgs e)
    {
        ServiceConfig.WithDefaultUpdateRoots(ServiceConfig.DefaultUpdateRoots.ToNotNull()
            .ConcatParams(new FileMediaSourceRootInfo()).ToArray());
        lbxDefaultUpdateRoots.ItemsSource = ServiceConfig.DefaultUpdateRoots;
    }

    private void BtnOk_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void BtnCancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}