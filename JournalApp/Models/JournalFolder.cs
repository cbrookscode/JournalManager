using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JournalApp.Models
{
    public class JournalFolder : JournalTreeItem
    {
        public int? ParentFolderId { get; set; } = null;

        public ObservableCollection<JournalTreeItem> Children { get; set; } = new();
    }
}
