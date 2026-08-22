using JournalApp.Data;
using JournalApp.Models;
using System;
using System.Collections;
using System.Collections.ObjectModel;
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
                }
                else if (treeItem is JournalFolder folder)
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
