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
        private ObservableCollection<JournalTreeItem> _folderTree;
        private Dictionary<int, JournalFolder> _folderDict;
        public AddFolderWindow(JournalDatabase journalDatabase)
        {
            InitializeComponent();
            _journalDatabase = journalDatabase;
            List<JournalTreeItem> treeInput = new List<JournalTreeItem>();
            treeInput.AddRange(_journalDatabase.GetAllEntries());
            treeInput.AddRange(_journalDatabase.GetAllFolders());

            (_folderTree, _folderDict) = _journalDatabase.SetupChildrenFolders(treeInput);
            FolderView.ItemsSource = _folderTree;
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

            JournalTreeItem treeItem = (JournalTreeItem)button.Tag;

            DeleteConfirmationWindow confirmationWindow = new DeleteConfirmationWindow("You are about to delete the following folder:", treeItem.Name);
            bool? result = confirmationWindow.ShowDialog();
            if (result == true)
            {
                if (treeItem is JournalEntry entry)
                {
                    _journalDatabase.DeleteEntry(entry.Id);
                    if (entry.FolderId != null)
                    {
                        if (_folderDict.TryGetValue(entry.FolderId.Value, out JournalFolder? parent))
                        {
                            if (parent != null)
                            {
                                parent.Children.Remove(entry);
                            }
                        }
                    }
                } else if (treeItem is JournalFolder folder)
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
                    }
                    else
                    {
                        _folderTree.Remove(folder);
                    }
                }
            }
        }
    }
}
