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
        public event Action<JournalFolder?>? FolderSelected;
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
            (bool success, List<JournalEntry> entries) = _journalDatabase.GetAllEntries();
            if (success)
            {
                treeInput.AddRange(entries);
            }
            (bool success2, List<JournalFolder> folders) = _journalDatabase.GetAllFolders();
            if (success2)
            {
                treeInput.AddRange(folders);
            }

            (_folderTree, _folderDict, _entryDict) = _journalDatabase.BuildJournalTree(treeInput);
            _journalDatabase.TreeItemCreated += HandleTreeItemCreated;
            _journalDatabase.TreeItemEdited += HandleTreeItemEdited;
            _journalDatabase.TreeItemDeleted += HandleTreeItemDeleted;
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

        public void ClearSelection()
        {
            if (SelectedItem != null)
            {
                if (_folderDict.TryGetValue(SelectedItem.Id, out JournalFolder? folder))
                {
                    TreeViewItem? item = FolderView.ItemContainerGenerator.ContainerFromItem(folder) as TreeViewItem;
                    if (item != null)
                    {
                        item.IsSelected = false;
                    }
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
                _folderDict[treeEntry.FolderId.Value].Children.Remove(treeEntry);
            }
            _entryDict.Remove(treeEntry.Id);
        }
        private void RemoveTreeBranchFromDictionaries(JournalFolder folder)
        {
            foreach (JournalTreeItem child in folder.Children)
            {
                if (child is JournalEntry entry)
                {
                    _entryDict.Remove(entry.Id);
                }
                else if (child is JournalFolder childFolder)
                {
                    RemoveTreeBranchFromDictionaries(childFolder);
                }
            }

            _folderDict.Remove(folder.Id);
        }
        private void DeleteTreeFolder(JournalFolder folder)
        {
            JournalFolder? parent = null;

            if (folder.ParentFolderId != null)
            {
                _folderDict.TryGetValue(
                    folder.ParentFolderId.Value,
                    out parent);
            }

            RemoveTreeBranchFromDictionaries(folder);

            if (parent != null)
            {
                parent.Children.Remove(folder);
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

            int? oldParentId = null;

            if (treeItem is JournalEntry entry)
            {
                oldParentId = entry.FolderId;
            }
            else if (treeItem is JournalFolder folder)
            {
                oldParentId = folder.ParentFolderId;
            }

            treeItem.Name = treeItem.RenameText;
            bool success = _journalDatabase.EditJournalTreeItem(treeItem, oldParentId);
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
                bool success = _journalDatabase.DeleteJournalTreeItem(treeItem);
            }
        }

        public void FolderView_SelectedItemChanged(object sender, RoutedEventArgs e)
        {
            if (FolderView.SelectedItem is JournalFolder folder)
            {
                FolderSelected?.Invoke(folder);
            } else
            {
                FolderSelected?.Invoke(null);
            }
        }

        private void HandleTreeItemCreated(JournalTreeItem treeItem)
        {
            if (treeItem is JournalEntry treeEntry)
            {
                _entryDict[treeEntry.Id] = treeEntry;
                if (treeEntry.FolderId == null)
                {
                    int insertIndex = 0;

                    while (insertIndex < _folderTree.Count &&
                           _folderTree[insertIndex] is JournalEntry)
                    {
                        insertIndex++;
                    }

                    _folderTree.Insert(insertIndex, treeEntry);
                } else
                {
                    int insertIndex = 0;

                    while (insertIndex < _folderDict[treeEntry.FolderId.Value].Children.Count &&
                           _folderDict[treeEntry.FolderId.Value].Children[insertIndex] is JournalEntry)
                    {
                        insertIndex++;
                    }
                    _folderDict[treeEntry.FolderId.Value].Children.Insert(insertIndex, treeEntry);
                }
            } else if (treeItem is JournalFolder treeFolder)
            {
                _folderDict[treeFolder.Id] = treeFolder;
                if (treeFolder.ParentFolderId == null)
                {
                    _folderTree.Add(treeFolder);
                } else
                {
                    _folderDict[treeFolder.ParentFolderId.Value].Children.Add(treeFolder);
                }
            }
        }

        private void HandleTreeItemDeleted(JournalTreeItem treeItem)
        {
            if (treeItem is JournalEntry entry)
            {
                DeleteTreeEntry(entry);
            }
            else if (treeItem is JournalFolder folder)
            {
                DeleteTreeFolder(folder);
            }
        }

        private void HandleTreeItemEdited(JournalTreeItem treeItem, int? oldParentId)
        {
            if (treeItem is JournalEntry entry)
            {
                MoveEntry(entry, oldParentId);
            }
        }
    }
}
