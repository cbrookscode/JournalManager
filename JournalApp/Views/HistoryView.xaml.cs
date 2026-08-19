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
            HistoryItems.ItemsSource = _journalDatabase.GetAllEntries();
        }
    }
}
