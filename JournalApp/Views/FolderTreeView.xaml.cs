using JournalApp.Data;
using JournalApp.Models;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

namespace JournalApp.Views
{
    /// <summary>
    /// Interaction logic for FolderTreeView.xaml
    /// </summary>
    public partial class FolderTreeView : UserControl
    {
        public IEnumerable? ItemsSource
        {
            get => FolderView.ItemsSource;
            set => FolderView.ItemsSource = value;
        }

        public JournalTreeItem? SelectedItem
        {
            get => FolderView.SelectedItem as JournalTreeItem;
        }
        private JournalDatabase _journalDatabase;
        private ObservableCollection<JournalTreeItem> _folderTree;
        private Dictionary<int, JournalFolder> _folderDict;
        private Dictionary<int, JournalEntry> _entryDict;

        public FolderTreeView()
        {
            InitializeComponent();
        }
        
        public void Initialize(JournalDatabase journalDatabase)
        {
            _journalDatabase = journalDatabase;
            List<JournalTreeItem> treeInput = new List<JournalTreeItem>();
            treeInput.AddRange(_journalDatabase.GetAllEntries());
            treeInput.AddRange(_journalDatabase.GetAllFolders());

            (_folderTree, _folderDict, _entryDict) = _journalDatabase.SetupChildrenFolders(treeInput);
            FolderView.ItemsSource = _folderTree;
        }

        public void SelectFolderById(int? folderId)
        {
            if (folderId == null)
            {
                return;
            }

            if (_folderDict.TryGetValue(folderId.Value, out JournalFolder? folder))
            {
                TreeViewItem? item = FolderView.ItemContainerGenerator.ContainerFromItem(folder) as TreeViewItem;
                if (item != null)
                {
                    item.IsSelected = true;
                }
            }
        }

        public void MoveEntry(JournalEntry entry, int? oldFolderId)
        {
            if (!_entryDict.TryGetValue(entry.Id, out JournalEntry? treeEntry))
            {
                return;
            }

            if (oldFolderId == null)
            {
                _folderTree.Remove(treeEntry);
            } else
            {
                _folderDict[oldFolderId.Value].Children.Remove(treeEntry);
            }

            if (entry.FolderId == null)
            {
                // moving to the root
                int insertIndex = 0;

                while (insertIndex < _folderTree.Count &&
                       _folderTree[insertIndex] is JournalEntry)
                {
                    insertIndex++;
                }

                _folderTree.Insert(insertIndex, treeEntry);
            } else
            {
                _folderDict[entry.FolderId.Value].Children.Add(treeEntry);
            }

            treeEntry.Name = entry.Name;
            treeEntry.FolderId = entry.FolderId;
            treeEntry.EType = entry.EType;
            treeEntry.Date = entry.Date;
            treeEntry.Notes = entry.Notes;
        }

        public void DeleteTreeEntry(JournalEntry entry)
        {
            if (!_entryDict.TryGetValue(entry.Id, out JournalEntry? treeEntry))
            {
                return;
            }

            if (treeEntry.FolderId == null)
            {
                _folderTree.Remove(treeEntry);
            } else
            {
                _entryDict.Remove(treeEntry.Id);
                _folderDict[treeEntry.FolderId.Value].Children.Remove(treeEntry);
            }
            _entryDict.Remove(treeEntry.Id);
        }

        public void DeleteTreeFolder(JournalFolder folder)
        {
            foreach (JournalTreeItem child in folder.Children) // delete children in folder if any
            {
                _journalDatabase.DeleteJournalTreeItem(child);
            }
            if (folder.ParentFolderId != null)
            {
                if (_folderDict.TryGetValue(folder.ParentFolderId.Value, out JournalFolder? parent))
                {
                    if (parent != null)
                    {
                        parent.Children.Remove(folder); // remove from parent children list if had a parent
                    }
                }
            }
            else
            {
                _folderTree.Remove(folder);
            }
        }
        public void Rename_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            JournalTreeItem treeItem = (JournalTreeItem)button.Tag;


            treeItem.RenameText = treeItem.Name;
            treeItem.IsRenaming = true;

            StackPanel? parent = button.Parent as StackPanel;

            if (parent != null)
            {
                TextBox? renameTextBox = parent
                    .Children
                    .OfType<TextBox>()
                    .FirstOrDefault();

                if (renameTextBox != null)
                {
                    renameTextBox.Dispatcher.BeginInvoke(() =>
                    {
                        renameTextBox.Focus();
                        renameTextBox.SelectAll();
                    });
                }
            }
        }
        public void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            JournalTreeItem treeItem = (JournalTreeItem)button.Tag;

            treeItem.IsRenaming = false;
        }

        public void Save_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            JournalTreeItem treeItem = (JournalTreeItem)button.Tag;

            SaveRename(treeItem);
        }

        public void EditEntry_Click(object sender, RoutedEventArgs e)
        {
            AddEntryWindow addEntryWindow = new AddEntryWindow();
            Button button = (Button)sender;
            JournalTreeItem treeItem = (JournalTreeItem)button.Tag;
            if (treeItem is JournalEntry entry)
            {
                addEntryWindow.Initialize(_journalDatabase, entry);
                addEntryWindow.EntrySaved += (savedEntry, oldFolderId) =>
                {
                    MoveEntry(savedEntry, oldFolderId);
                };

                bool? result = addEntryWindow.ShowDialog();
            }
        }

        private void RenameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            if (textBox.DataContext is not JournalTreeItem treeItem)
                return;

            if (e.Key == Key.Enter)
            {
                SaveRename(treeItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                treeItem.IsRenaming = false;
                e.Handled = true;
            }
        }

        private void SaveRename(JournalTreeItem treeItem)
        {
            if (string.IsNullOrWhiteSpace(treeItem.RenameText))
                return;

            treeItem.Name = treeItem.RenameText;
            bool success = _journalDatabase.EditJournalTreeItem(treeItem);
            if (success)
            {
                treeItem.IsRenaming = false;
            }
            else
            {
                Debug.WriteLine("Failed to save tree item to db");
            }
        }

        public void Delete_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            JournalTreeItem treeItem = (JournalTreeItem)button.Tag;

            DeleteConfirmationWindow confirmationWindow = new DeleteConfirmationWindow("You are about to delete the following, and if is a folder all nested items:", treeItem.Name);
            bool? result = confirmationWindow.ShowDialog();
            if (result == true)
            {
                if (treeItem is JournalEntry entry)
                {
                    _journalDatabase.DeleteEntry(entry.Id);
                    DeleteTreeEntry(entry);
                }
                else if (treeItem is JournalFolder folder)
                {
                    _journalDatabase.DeleteFolder(folder.Id); // delete folder
                    DeleteTreeFolder(folder);
                }
            }
        }
    }
}
