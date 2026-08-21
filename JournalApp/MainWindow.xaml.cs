using JournalApp.Data;
using JournalApp.Models;
using JournalApp.Views;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace JournalApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private JournalDatabase _journalDatabase;
        public MainWindow()
        {
            InitializeComponent();

            _journalDatabase = new JournalDatabase();
            _journalDatabase.Initialize();
            ShowJournalExplorerView();
        }

        private void NewEntry_Click(object sender, RoutedEventArgs e)
        {
            ShowEntryView();
        }

        private void History_Click(object sender, RoutedEventArgs e)
        {
            ShowHistoryView();
        }

        private void JournalExplorer_Click(object sender, RoutedEventArgs e)
        {
            ShowJournalExplorerView();
        }

        private void ShowHistoryView()
        {
            HistoryView historyView = new HistoryView(_journalDatabase);
            MainContent.Content = historyView;
        }
        private void ShowEntryView()
        {
            JournalEntryView journalEntryView = new JournalEntryView(_journalDatabase);
            journalEntryView.EntrySaved += HandleEntrySaved;
            MainContent.Content = journalEntryView;
        }

        private void ShowJournalExplorerView()
        {
            JournalExplorerView journalExplorerView = new JournalExplorerView(_journalDatabase);
            MainContent.Content = journalExplorerView;
        }

        /// <summary>
        /// Handler for event subscription to EntrySaved when creating a new JournalEntryView
        /// </summary>
        /// <param name="journalEntry"></param>
        private void HandleEntrySaved(JournalEntry journalEntry)
        {
            ShowHistoryView();
        }
    }
}