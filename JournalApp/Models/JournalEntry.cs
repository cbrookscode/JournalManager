using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JournalApp.Models
{
    public class JournalEntry : JournalTreeItem
    {
        private int? _folderId;
        private EntryType _eType;
        private DateOnly _date;
        private string _notes  = "";

        public EntryType EType
        {
            get => _eType;
            set
            {
                if (_eType != value)
                {
                    _eType = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateOnly Date
        {
            get => _date;
            set
            {
                if (_date != value)
                {
                    _date = value;
                    OnPropertyChanged();
                }
            }
        }
        public string Notes
        {
            get => _notes;
            set
            {
                if (_notes != value)
                {
                    _notes = value;
                    OnPropertyChanged();
                }
            }
        }

        public int? FolderId
        {
            get => _folderId;
            set
            {
                if (_folderId != value)
                {
                    _folderId = value;
                    OnPropertyChanged();
                }
            }
        }

    }
}