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
using JournalApp.Models;

namespace JournalApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            EntryTypeComboBox.ItemsSource = Enum.GetValues<EntryType>();
            EntryTypeComboBox.SelectedItem = EntryType.Daily;
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
            //Debug.WriteLine("button clicked!");
            //Debug.WriteLine($"Title: {NewEntry.Title}");
            //Debug.WriteLine($"Date: {NewEntry.Date}");
            //Debug.WriteLine($"Entry Type: {NewEntry.EType}");
            //Debug.WriteLine($"Notes: {NewEntry.Notes}");
        }
    }
}