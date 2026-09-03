using JournalApp.Data;
using JournalApp.Models;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JournalApp.Views
{
    /// <summary>
    /// Interaction logic for JournalExplorerView.xaml
    /// </summary>
    public partial class JournalExplorerView : UserControl
    {
        public JournalFolder CreatedFolder { get; set; } = new JournalFolder();
        private JournalDatabase _journalDatabase;
        public JournalExplorerView(JournalDatabase journalDatabase)
        {
            InitializeComponent();
            _journalDatabase = journalDatabase;
            FolderTree.Initialize(journalDatabase);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FolderName.Text))
            {
                return;
            }

            CreatedFolder.Name = FolderName.Text;
            CreatedFolder.ParentFolderId = null;

            if (FolderTree.SelectedItem is JournalFolder selectedFolder)
            {
                CreatedFolder.ParentFolderId = selectedFolder.Id;
            }
            bool success = _journalDatabase.CreateJournalTreeItem(CreatedFolder);
            if (success)
            {
                FolderName.Text = "";
                CreatedFolder = new JournalFolder();
            }
            else
            {
                Debug.WriteLine("Couldn't save folder in database");
            }
        }

        private void Edit_Name(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            JournalTreeItem item = (JournalTreeItem)button.Tag;
        }
    }
}
