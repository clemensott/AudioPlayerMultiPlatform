using AudioPlayerBackend.Audio;
using AudioPlayerBackend.Build;
using StdOttStandard.TaskCompletionSources;
using Windows.UI.Xaml.Controls;

namespace AudioPlayerFrontend.Extensions
{
    static class PageExtensions
    {
        internal static void NavigateToMainPage(this Frame frame, ServiceHandler parameter)
        {
            frame.Navigate(typeof(MainPage), parameter);
        }

        internal static void NavigateToSearchPage(this Frame frame, IAudioService parameter)
        {
            frame.Navigate(typeof(SearchPage), parameter);
        }

        internal static void NavigateToBuildOpenPage(this Frame frame, ServiceHandler parameter)
        {
            frame.Navigate(typeof(BuildOpenPage), parameter);
        }

        internal static void NavigateToSettingsPage(this Frame frame, TaskCompletionSourceS<ServiceBuilder> parameter)
        {
            frame.Navigate(typeof(SettingsPage), parameter);
        }
    }
}
