using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JournalApp.Models
{
    public class JournalFolder
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int? ParentFolderId { get; set; } = null;

        public ObservableCollection<JournalFolder> Children { get; set; } = new();
    }
}
