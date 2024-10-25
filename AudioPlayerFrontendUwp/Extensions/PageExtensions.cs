using AudioPlayerBackend.Build;
using AudioPlayerBackend.ViewModels;
using StdOttStandard.TaskCompletionSources;
using Windows.UI.Xaml.Controls;

namespace AudioPlayerFrontend.Extensions
{
    static class PageExtensions
    {
        internal static void NavigateToMainPage(this Frame frame, AudioServicesHandler parameter)
        {
            frame.Navigate(typeof(MainPage), parameter);
        }

        internal static void NavigateToSearchPage(this Frame frame, ISongSearchViewModel parameter)
        {
            frame.Navigate(typeof(SearchPage), parameter);
        }

        internal static void NavigateToBuildOpenPage(this Frame frame, AudioServicesHandler parameter)
        {
            frame.Navigate(typeof(BuildOpenPage), parameter);
        }

        internal static void NavigateToSettingsPage(this Frame frame, TaskCompletionSourceS<AudioServicesBuildConfig> parameter)
        {
            frame.Navigate(typeof(SettingsPage), parameter);
        }
    }
}
