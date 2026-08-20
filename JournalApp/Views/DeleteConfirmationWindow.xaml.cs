using JournalApp.Models;
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

namespace JournalApp.Views
{
    /// <summary>
    /// Interaction logic for DeleteConfirmationWindow.xaml
    /// </summary>
    public partial class DeleteConfirmationWindow : Window
    {
        public DeleteConfirmationWindow(JournalEntry entry)
        {
            InitializeComponent();
            DataContext = entry;
        }

        public void Delete_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        public void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult= false;
        }
    }
}
