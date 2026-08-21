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
            FolderView.ItemsSource = _journalDatabase.SetupChildrenFolders(_journalDatabase.GetAllFolders());
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
