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
        private JournalEntry _displayedEntry;
        public MainWindow()
        {
            InitializeComponent();

            _journalDatabase = new JournalDatabase();
            _journalDatabase.Initialize();
            ShowJournalExplorerView();
        }

        private void NewEntry_Click(object sender, RoutedEventArgs e)
        {
            ShowNewEntryView();
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
        private void ShowNewEntryView()
        {
            JournalEntryView journalEntryView = new JournalEntryView();
            journalEntryView.Initialize(_journalDatabase);
            journalEntryView.EntrySaved += HandleNewEntrySaved;
            MainContent.Content = journalEntryView;
        }

        private void ShowJournalExplorerView()
        {
            JournalExplorerView journalExplorerView = new JournalExplorerView(_journalDatabase);
            journalExplorerView.TreeItemDoubleClicked += HandleJournalTreeItemDoubleClicked;
            journalExplorerView.TreeItemDeleted += HandleTreeItemDeleted;
            SidebarContent.Content = journalExplorerView;
            MainContent.Content = null;
        }

        private void HandleJournalTreeItemDoubleClicked(JournalTreeItem treeItem)
        {
            if (treeItem is JournalEntry entry)
            {
                Debug.WriteLine("tree item is an entry");
                JournalEntryView journalEntryView = new JournalEntryView();
                journalEntryView.Initialize(_journalDatabase);
                journalEntryView.LoadEntry(entry);
                _displayedEntry = entry;
                MainContent.Content = journalEntryView;
            }
        }

        private void HandleTreeItemDeleted(JournalTreeItem treeItem)
        {
            if (treeItem == null) return;
            if (treeItem.Id == _displayedEntry.Id)
            {
                MainContent.Content = null;
            }
        }

        private void HandleNewEntrySaved(JournalEntry entry, int? id)
        {
            _displayedEntry = entry;
        }
    }
}