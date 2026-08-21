using JournalApp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using JournalApp.Models;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace JournalApp.Views
{
    /// <summary>
    /// Interaction logic for AddFolderWindow.xaml
    /// </summary>
    public partial class AddFolderWindow : Window
    {
        public JournalFolder CreatedFolder { get; set; } = new JournalFolder();

        private JournalDatabase _journalDatabase;
        private ObservableCollection<JournalFolder> _folders;
        private Dictionary<int, JournalFolder> _folderDict;
        public AddFolderWindow(JournalDatabase journalDatabase)
        {
            InitializeComponent();
            _journalDatabase = journalDatabase;
            (_folders, _folderDict) = _journalDatabase.SetupChildrenFolders(_journalDatabase.GetAllFolders());
            FolderView.ItemsSource = _folders;
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FolderName.Text))
            {
                DialogResult = false;
                return;
            }

            CreatedFolder.Name = FolderName.Text;
            CreatedFolder.ParentFolderId = null;

            if (FolderView.SelectedItem is JournalFolder selectedFolder)
            {
                CreatedFolder.ParentFolderId = selectedFolder.Id;
            }
            DialogResult = true;
        }

        public void Delete_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            JournalFolder folder = (JournalFolder)button.Tag;

            DeleteConfirmationWindow confirmationWindow = new DeleteConfirmationWindow("You are about to delete the following folder:", folder.Name);
            bool? result = confirmationWindow.ShowDialog();
            if (result == true)
            {
                _journalDatabase.DeleteFolder(folder.Id);
                if (folder.ParentFolderId != null)
                {
                    if (_folderDict.TryGetValue(folder.ParentFolderId.Value, out JournalFolder? parent))
                    {
                        if (parent != null)
                        {
                            parent.Children.Remove(folder);
                        }
                    }
                } else
                {
                    _folders.Remove(folder);
                }
            }
        }
    }
}
