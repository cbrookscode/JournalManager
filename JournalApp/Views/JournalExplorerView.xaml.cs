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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JournalApp.Views
{
    /// <summary>
    /// Interaction logic for JournalExplorerView.xaml
    /// </summary>
    public partial class JournalExplorerView : UserControl
    {
        private JournalDatabase _journalDatabase;
        public JournalExplorerView(JournalDatabase journalDatabase)
        {
            InitializeComponent();
            _journalDatabase = journalDatabase;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ShowAddFolderWindow();
        }

        private void ShowAddFolderWindow()
        {
            AddFolderWindow addFolderWindow = new AddFolderWindow(_journalDatabase);
            bool? result = addFolderWindow.ShowDialog();
            if (result == true)
            {
                _journalDatabase.CreateFolder(addFolderWindow.CreatedFolder); // is the folder that was created by the window, need to add to database
            }
        }
    }
}
