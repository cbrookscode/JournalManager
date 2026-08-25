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

            (_folderTree, _folderDict) = _journalDatabase.SetupChildrenFolders(treeInput);
            FolderView.ItemsSource = _folderTree;
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
                }
                else if (treeItem is JournalFolder folder)
                {
                    _journalDatabase.DeleteFolder(folder.Id); // delete folder
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
            }
        }
    }
}
