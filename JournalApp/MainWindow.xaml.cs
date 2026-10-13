using JournalApp.Data;
using JournalApp.Models;
using JournalApp.Views;
using Microsoft.Win32;
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
            if (treeItem == null || _displayedEntry == null) return;
            if (treeItem is JournalFolder) return;
            if (treeItem.Id == _displayedEntry.Id)
            {
                MainContent.Content = null;
            }
        }

        private void HandleNewEntrySaved(JournalEntry entry, int? id)
        {
            _displayedEntry = entry;
        }

        private void Snapshot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = _journalDatabase.CreateSnapshot();

                MessageBox.Show(
                    $"Snapshot created successfully:\n{path}",
                    "Snapshot Complete");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to create snapshot:\n{ex.Message}",
                    "Snapshot Error");
            }
        }

        private void ImportDatabase_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Import Journal Database",
                Filter = "SQLite Database (*.db)|*.db",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            MessageBoxResult confirmation = MessageBox.Show(
                "Importing this database will replace your current journal.\n\n" +
                "A snapshot of your current database will be created automatically.\n\n" +
                "Do you want to continue?",
                "Confirm Database Import",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                string backupPath =
                    _journalDatabase.ImportDatabase(dialog.FileName);

                MessageBox.Show(
                    "Database imported successfully!\n\n" +
                    $"Previous database saved to:\n{backupPath}",
                    "Import Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Restart application
                string executablePath = Environment.ProcessPath
                    ?? throw new InvalidOperationException(
                        "Could not determine application path.");

                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = executablePath,
                        UseShellExecute = true
                    });

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Database import failed:\n{ex.Message}\n\n" +
                    "If a snapshot was created, it remains available for recovery.",
                    "Import Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}