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
using JournalApp.Data;
using JournalApp.Models;

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

            EntryTypeComboBox.ItemsSource = Enum.GetValues<EntryType>();
            EntryTypeComboBox.SelectedItem = EntryType.Daily;
            _journalDatabase = new JournalDatabase();
            _journalDatabase.Initialize();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DateOnly date = DateOnly.FromDateTime(DateTime.Now);
            if (EntryDate.SelectedDate != null)
            {
                date = DateOnly.FromDateTime((DateTime) EntryDate.SelectedDate);
            }

            JournalEntry NewEntry = new JournalEntry {
                Title = JournalTitle.Text,
                EType = (EntryType) EntryTypeComboBox.SelectedItem,
                Date = date,
                Notes = JournalNotes.Text
            };
            _journalDatabase.SaveEntry(NewEntry);
            List<JournalEntry> entries = _journalDatabase.GetAllEntries();
            for (int i = 0; i < entries.Count; i++)
            {
                Debug.WriteLine($"{entries[i].Id}");
                Debug.WriteLine($"{entries[i].Title}");
                Debug.WriteLine($"{entries[i].EType}");
                Debug.WriteLine($"{entries[i].Date}");
                Debug.WriteLine($"{entries[i].Notes}");
            }
        }
    }
}