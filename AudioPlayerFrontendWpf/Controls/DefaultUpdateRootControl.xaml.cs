using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.FileSystem;
using AudioPlayerFrontend.Join;
using System;
using System.Windows;
using System.Windows.Controls;

namespace AudioPlayerFrontend.Controls
{
    /// <summary>
    /// Interaktionslogik für DefaultUpdateRootControl.xaml
    /// </summary>
    public partial class DefaultUpdateRootControl : UserControl
    {
        private bool isUpdatingValue;

        public event EventHandler<FileMediaSourceRootInfo> ValueChanged;

        public DefaultUpdateRootControl()
        {
            InitializeComponent();

            foreach (LocalKnownFolder knownFolder in UpdateLibraryService.GetLocalKnownFolders())
            {
                cbxKnownFolder.Items.Add(new ComboBoxItem()
                {
                    DataContext = knownFolder.Value,
                    Content = knownFolder.Name,
                });
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ApplyDataContext();
        }

        private void ApplyDataContext()
        {
            if (isUpdatingValue) return;

            try
            {
                isUpdatingValue = true;

                FileMediaSourceRootInfo value = DataContext is FileMediaSourceRootInfo ? (FileMediaSourceRootInfo)DataContext : new FileMediaSourceRootInfo();

                tbxName.Text = value.Name;
                cbxWithSubFolders.IsChecked = value.UpdateType.HasFlag(FileMediaSourceRootUpdateType.Folders);

                switch (value.PathType)
                {
                    case FileMediaSourceRootPathType.KnownFolder:
                        rbnKnownFolder.IsChecked = true;
                        cbxKnownFolder.SelectedValue = value.Path;

                        rbnPath.IsChecked = false;
                        tbxPath.Text = string.Empty;
                        break;

                    case FileMediaSourceRootPathType.Path:
                        rbnPath.IsChecked = true;
                        tbxPath.Text = value.Path;

                        rbnKnownFolder.IsChecked = false;
                        cbxKnownFolder.SelectedValue = null;
                        break;
                }
            }
            finally
            {
                isUpdatingValue = false;
            }
        }

        private void TriggerValueChanged()
        {
            if (isUpdatingValue) return;

            try
            {
                isUpdatingValue = true;

                FileMediaSourceRootPathType pathType;
                string path;
                if (rbnKnownFolder.IsChecked == true)
                {
                    pathType = FileMediaSourceRootPathType.KnownFolder;
                    path = (string)cbxKnownFolder.SelectedValue;
                }
                else
                {
                    pathType = FileMediaSourceRootPathType.Path;
                    path = tbxPath.Text;
                }

                ValueChanged?.Invoke(this, new FileMediaSourceRootInfo(
                    cbxWithSubFolders.IsChecked == true
                        ? FileMediaSourceRootUpdateType.Songs | FileMediaSourceRootUpdateType.Folders
                        : FileMediaSourceRootUpdateType.Songs,
                    tbxName.Text,
                    pathType,
                    path
                ));
            }
            finally
            {
                isUpdatingValue = false;
            }
        }

        private void TbxName_TextChanged(object sender, TextChangedEventArgs e)
        {
            TriggerValueChanged();
        }

        private void CbxWithSubFolders_Checked(object sender, RoutedEventArgs e)
        {
            rbnPath.IsChecked = false;
            TriggerValueChanged();
        }

        private void CbxWithSubFolders_Unchecked(object sender, RoutedEventArgs e)
        {
            TriggerValueChanged();
        }

        private void RbnKnownFolder_Checked(object sender, RoutedEventArgs e)
        {
            rbnPath.IsChecked = false;
            TriggerValueChanged();
        }

        private void CbxKnownFolder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TriggerValueChanged();
        }

        private void RbnPath_Checked(object sender, RoutedEventArgs e)
        {
            rbnKnownFolder.IsChecked = false;
            TriggerValueChanged();
        }

        private void TbxPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            TriggerValueChanged();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ApplyDataContext();
        }
    }
}
