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
using System.Windows.Shapes;
using JournalApp.Data;
using JournalApp.Models;

namespace JournalApp.Views
{
    /// <summary>
    /// Interaction logic for AddEntryWindow.xaml
    /// </summary>
    public partial class AddEntryWindow : Window
    {
        public event Action<JournalEntry, int?>? EntrySaved;
        public AddEntryWindow()
        {
            InitializeComponent();
        }

        public void Initialize(JournalDatabase database, JournalEntry? entry)
        {
            AddEntryView.Initialize(database);
            if (entry != null)
            {
                AddEntryView.LoadEntry(entry);
            }
            AddEntryView.EntrySaved += (savedEntry, oldFolderId) => {
                EntrySaved?.Invoke(savedEntry, oldFolderId);
                DialogResult = true;
            };
        }
    }
}
