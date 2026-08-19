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
    /// Interaction logic for HistoryView.xaml
    /// </summary>
    public partial class HistoryView : UserControl
    {
        private JournalDatabase _journalDatabase;
        public HistoryView(JournalDatabase journalDatabase)
        {
            InitializeComponent();

            _journalDatabase = journalDatabase;
            RefreshEntries();
        }

        public void Delete_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            int id = (int)button.Tag;

            _journalDatabase.DeleteEntry(id);
            RefreshEntries();
        }

        private void RefreshEntries()
        {
            HistoryItems.ItemsSource = _journalDatabase.GetAllEntries();
        }
    }
}
