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
        public event Action<JournalEntry>? EntrySaved;

        private JournalDatabase _journalDatabase;

        public JournalEntryView(JournalDatabase journalDatabase)
        {
            InitializeComponent();

            _journalDatabase = journalDatabase;
            EntryTypeComboBox.ItemsSource = Enum.GetValues<EntryType>();
            EntryTypeComboBox.SelectedItem = EntryType.Daily;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DateOnly date = DateOnly.FromDateTime(DateTime.Now);
            if (EntryDate.SelectedDate != null)
            {
                date = DateOnly.FromDateTime((DateTime)EntryDate.SelectedDate);
            }

            JournalEntry NewEntry = new JournalEntry
            {
                Name = JournalTitle.Text,
                EType = (EntryType)EntryTypeComboBox.SelectedItem,
                Date = date,
                Notes = JournalNotes.Text
            };
            _journalDatabase.SaveEntry(NewEntry);
            EntrySaved?.Invoke(NewEntry);
        }
    }
}
