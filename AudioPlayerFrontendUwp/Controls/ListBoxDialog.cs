using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace AudioPlayerFrontend.Controls
{
    class ListBoxDialog<T>
    {
        private readonly ListBox list;
        private readonly ContentDialog dialog;
        private readonly TaskCompletionSource<T> taskCompletionSource;

        private ListBoxDialog(IEnumerable<ListBoxDialogItem<T>> items, T selectedValue, object title)
        {
            list = new ListBox();
            foreach (ListBoxDialogItem<T> item in items)
            {
                list.Items.Add(new ListBoxItem()
                {
                    DataContext = item.Value,
                    Content = item.Content,
                });

                if (Equals(selectedValue, item.Value)) list.SelectedIndex = list.Items.Count - 1;
            }

            list.SelectionChanged += List_SelectionChanged;

            dialog = new ContentDialog()
            {
                Content = list,
                Title = title,
                IsPrimaryButtonEnabled = true,
                PrimaryButtonText = "Cancel",
                IsSecondaryButtonEnabled = false,
            };

            taskCompletionSource = new TaskCompletionSource<T>();
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                taskCompletionSource.SetResult((T)e.AddedItems.Cast<FrameworkElement>().First().DataContext);
                dialog.Hide();
            }
        }

        private async void Start()
        {
            ContentDialogResult result = await dialog.ShowAsync();

            if (result != ContentDialogResult.None) taskCompletionSource.SetResult(default(T));
        }

        public static Task<T> Start<T>(IEnumerable<ListBoxDialogItem<T>> items, T selectedValue, object title = null)
        {
            ListBoxDialog<T> dialog = new ListBoxDialog<T>(items, selectedValue, title);
            dialog.Start();

            return dialog.taskCompletionSource.Task;
        }
    }
}
