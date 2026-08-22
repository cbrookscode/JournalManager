namespace JournalApp.Models
{
    public class JournalEntry : JournalTreeItem
    {
        public int? FolderId { get; set; }
        public EntryType EType { get; set; }
        public DateOnly Date { get; set; }
        public String Notes { get; set; } = "";
    }
}