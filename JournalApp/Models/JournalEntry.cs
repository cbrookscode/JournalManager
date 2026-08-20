namespace JournalApp.Models
{
    public class JournalEntry
    {
        public int Id { get; set; }
        public int? FolderId { get; set; }
        public String Title { get; set; } = "";
        public EntryType EType { get; set; }
        public DateOnly Date { get; set; }
        public String Notes { get; set; } = "";
    }
}