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
using System.Windows.Shapes;


namespace JournalApp.Views
{
    /// <summary>
    /// Interaction logic for JournalEntryView.xaml
    /// </summary>
    public partial class JournalEntryView : UserControl
    {
        public event Action<JournalEntry, int?>? EntrySaved;

        private JournalDatabase _journalDatabase;
        private JournalEntry? _editingEntry;
        public JournalEntry? EditingEntry => _editingEntry;

        public JournalEntryView()
        {
            InitializeComponent();
        }

        public void Initialize(JournalDatabase journalDatabase)
        {
            _journalDatabase = journalDatabase;
            EntryTypeComboBox.ItemsSource = Enum.GetValues<EntryType>();
            EntryTypeComboBox.SelectedItem = EntryType.Daily;
            FolderTree.Initialize(journalDatabase);
            FolderTree.FolderSelected += (folder) =>
            {
                SelectedFolderText.Text =
                    folder == null
                        ? "No Folder Selected"
                        : folder.Name;
            };
            FolderExpander.LostKeyboardFocus += (_, _) =>
            {
                if (!FolderExpander.IsKeyboardFocusWithin)
                {
                    FolderExpander.IsExpanded = false;
                }
            };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            if (_editingEntry == null)
            {
                SaveNewEntry();
            } else
            {
                SaveEditedEntry(_editingEntry.Id);
            }
            
        }

        private void SaveNewEntry()
        {
            DateOnly date = DateOnly.FromDateTime(DateTime.Now);
            if (EntryDate.SelectedDate != null)
            {
                date = DateOnly.FromDateTime((DateTime)EntryDate.SelectedDate);
            }

            JournalEntry NewEntry = new JournalEntry
            {
                Name = JournalTitle.Text,
                FolderId = null,
                EType = (EntryType)EntryTypeComboBox.SelectedItem,
                Date = date,
                Notes = JournalNotes.Text
            };

            if (FolderTree.SelectedItem is JournalFolder folder)
            {
                NewEntry.FolderId = folder.Id;
            }
            bool success = _journalDatabase.CreateJournalTreeItem(NewEntry);
            if (success)
            {
                EntrySaved?.Invoke(NewEntry, null);
            } else
            {
                Debug.WriteLine("Couldn't save entry in database");
            }
            LoadEntry(NewEntry);
        }

        private void SaveEditedEntry(int Id)
        {
            int? oldFolderId = _editingEntry.FolderId;

            DateOnly date = DateOnly.FromDateTime(DateTime.Now);
            if (EntryDate.SelectedDate != null)
            {
                date = DateOnly.FromDateTime((DateTime)EntryDate.SelectedDate);
            }

            _editingEntry.Name = JournalTitle.Text;
            _editingEntry.EType = (EntryType)EntryTypeComboBox.SelectedItem;
            _editingEntry.Date = date;
            _editingEntry.Notes = JournalNotes.Text;

            int? newFolderId = null;
            if (FolderTree.SelectedItem is JournalFolder folder)
            {
                newFolderId = folder.Id;
            }
            _editingEntry.FolderId = newFolderId;

            bool success = _journalDatabase.EditJournalTreeItem(_editingEntry, oldFolderId);
            if (success)
            {
                EntrySaved?.Invoke(_editingEntry, oldFolderId);
            } else
            {
                Debug.WriteLine("failed to edit entry");
            }

            LoadEntry(_editingEntry);
        }

        public void LoadEntry(JournalEntry journalEntry)
        {
            _editingEntry = journalEntry;

            JournalTitle.Text = journalEntry.Name;
            EntryDate.SelectedDate = journalEntry.Date.ToDateTime(TimeOnly.MinValue);
            JournalNotes.Text = journalEntry.Notes;
            EntryTypeComboBox.SelectedItem = journalEntry.EType;

            FolderTree.SetSelectedFolderById(journalEntry.FolderId);
        }

        private void Clear_Selection(object sender, RoutedEventArgs e)
        {
            FolderTree.ClearSelection();
            SelectedFolderText.Text = "No Folder Selected";
        }
    }
}
