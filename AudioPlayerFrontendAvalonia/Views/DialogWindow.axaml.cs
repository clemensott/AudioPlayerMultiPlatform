using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AudioPlayerFrontendAvalonia.Views;

public partial class DialogWindow : Window
{
    public enum DialogResult
    {
        Primary,
        Secondary,
        Cancel,
    }

    public string? Text
    {
        get => tblText.Text;
        set => tblText.Text = value;
    }

    public string? PrimaryButtonText
    {
        get => btnPrimary.Content as string;
        set => btnPrimary.Content = value;
    }

    public string? SecondaryButtonText
    {
        get => btnSecondary.Content as string;
        set => btnSecondary.Content = value;
    }

    public DialogWindow()
    {
        InitializeComponent();
    }

    public DialogWindow(string? text, string? title, string primaryButtonText, string? secondaryButtonText) : this()
    {
        Text = text;
        Title = title;
        PrimaryButtonText = primaryButtonText;
        if (secondaryButtonText is null) btnSecondary.IsVisible = false;
        else SecondaryButtonText = secondaryButtonText;
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
    }

    private void BtnPrimary_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(DialogResult.Primary);
    }

    private void BtnSecondary_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(DialogResult.Secondary);
    }

    public static Task<DialogResult> Show(Window ownerWindow, string text, string? title = null,
        string primaryButtonText = "Ok", string? secondaryButtonText = "Cancel")
    {
        DialogWindow dialog = new DialogWindow(text, title, primaryButtonText, secondaryButtonText);
        return dialog.ShowDialog<DialogResult>(ownerWindow);
    }

    public static Task<DialogResult> ShowPrimary(Window ownerWindow, string text, string? title = null,
        string primaryButtonText = "Ok")
    {
        return Show(ownerWindow, text, title, primaryButtonText, null);
    }
}