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
        public AddFolderWindow(JournalDatabase journalDatabase)
        {
            InitializeComponent();
            _journalDatabase = journalDatabase;
            FolderTree.Initialize(journalDatabase);
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

            if (FolderTree.SelectedItem is JournalFolder selectedFolder)
            {
                CreatedFolder.ParentFolderId = selectedFolder.Id;
            }
            DialogResult = true;
        }
    }
}
