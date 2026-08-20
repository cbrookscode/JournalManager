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

namespace JournalApp.Views
{
    /// <summary>
    /// Interaction logic for AddFolderWindow.xaml
    /// </summary>
    public partial class AddFolderWindow : Window
    {
        private JournalDatabase _journalDatabase;
        public AddFolderWindow(JournalDatabase journalDatabase)
        {
            InitializeComponent();
            _journalDatabase = journalDatabase;
            ParentFolderOptions.ItemsSource = _journalDatabase.GetAllFolders();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            JournalFolder folder = new JournalFolder
            {
                Name = FolderName.Name,
                ParentFolderId = ParentFolderOptions.Text // need to make name for the field unique so that i can lookup by name and grab the parents id
            };
            _journalDatabase.CreateFolder()
        }
    }
}
