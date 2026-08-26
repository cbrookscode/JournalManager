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
        private JournalEntry? _editingeEntry;

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

            if (_editingeEntry == null)
            {
                Debug.WriteLine("Editing entry is null");
                SaveNewEntry();
            } else
            {
                Debug.WriteLine("Editing entry is not null");
                SaveEditedEntry(_editingeEntry.Id);
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
            _journalDatabase.SaveEntry(NewEntry);
            EntrySaved?.Invoke(NewEntry, null);
        }

        private void SaveEditedEntry(int Id)
        {
            int? oldFolderId = _editingeEntry.FolderId;

            DateOnly date = DateOnly.FromDateTime(DateTime.Now);
            if (EntryDate.SelectedDate != null)
            {
                date = DateOnly.FromDateTime((DateTime)EntryDate.SelectedDate);
            }

            _editingeEntry.Name = JournalTitle.Text;
            _editingeEntry.EType = (EntryType)EntryTypeComboBox.SelectedItem;
            _editingeEntry.Date = date;
            _editingeEntry.Notes = JournalNotes.Text;

            int? newFolderId = null;
            if (FolderTree.SelectedItem is JournalFolder folder)
            {
                newFolderId = folder.Id;
            }
            _editingeEntry.FolderId = newFolderId;

            bool success = _journalDatabase.EditJournalTreeItem(_editingeEntry);
            if (success)
            {
                EntrySaved?.Invoke(_editingeEntry, oldFolderId);
            } else
            {
                Debug.WriteLine("failed to edit entry");
            }
            _editingeEntry = null;
        }

        public void LoadEntry(JournalEntry journalEntry)
        {
            _editingeEntry = journalEntry;

            JournalTitle.Text = journalEntry.Name;
            EntryDate.SelectedDate = journalEntry.Date.ToDateTime(TimeOnly.MinValue);
            JournalNotes.Text = journalEntry.Notes;
            EntryTypeComboBox.SelectedItem = journalEntry.EType;
            FolderTree.SelectFolderById(journalEntry.FolderId);
        }

        private void Clear_Selection(object sender, RoutedEventArgs e)
        {
            FolderTree.ClearSelection();
        }
    }
}
