using Microsoft.Data.Sqlite;
using System.IO;
using JournalApp.Models;

namespace JournalApp.Data
{
    public class JournalDatabase
    {
        private string _localAppDatapath;
        private string _appDataDirectory;
        private string _databasePath;
        public JournalDatabase() 
        {
            _localAppDatapath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _appDataDirectory = Path.Combine(_localAppDatapath, "JournalApp");
            _databasePath = Path.Combine(_appDataDirectory, "journal.db");
        }

        public void Initialize()
        {
            Directory.CreateDirectory(_appDataDirectory); // wont create if already exists

            using SqliteConnection _connection = 
                new SqliteConnection($"Data Source={_databasePath}"); // creates db if it doesn't exist based source provided

            _connection.Open();
            SqliteCommand command = _connection.CreateCommand();
            command.CommandText =
                """
                CREATE TABLE IF NOT EXISTS JournalEntries (
                    Id INTEGER PRIMARY KEY,
                    Title TEXT NOT NULL,
                    EntryType INTEGER NOT NULL,
                    EntryDate TEXT NOT NULL,
                    Notes TEXT NOT NULL
                );
                """;
            command.ExecuteNonQuery();
        }

        public void SaveEntry(JournalEntry journalEntry)
        {
            using SqliteConnection _connection = 
                new SqliteConnection($"Data Source={_databasePath}"); // creates db if it doesn't exist based source provided

            _connection.Open();
            SqliteCommand command = _connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO JournalEntries (Title, EntryType, EntryDate, Notes)
                VALUES($title, $entryType, $entryDate, $notes);
                """;
            command.Parameters.AddWithValue("$title", journalEntry.Title);
            command.Parameters.AddWithValue("$entryType", (int)journalEntry.EType);
            command.Parameters.AddWithValue("$entryDate", journalEntry.Date.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("notes", journalEntry.Notes);
            command.ExecuteNonQuery();
            _connection.Close();
        }

        public List<JournalEntry> GetAllEntries()
        {
            using SqliteConnection connection =
                new SqliteConnection($"Data Source={_databasePath}");

            connection.Open();
            SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT * FROM JournalEntries;
                """;
            using SqliteDataReader reader = command.ExecuteReader();

            List<JournalEntry> entries = new List<JournalEntry>();
            while (reader.Read())
            {
                JournalEntry entry = ConvertToJournalEntry(reader);
                entries.Add(entry);
            }
            return entries;
        }

        private JournalEntry ConvertToJournalEntry(SqliteDataReader reader)
        {

            JournalEntry entry = new JournalEntry();

            entry.Id = reader.GetInt32(reader.GetOrdinal("Id"));
            entry.Title = reader.GetString(reader.GetOrdinal("Title")); // get the column position and then get the string stored at the position explicitly 
            entry.EType = (EntryType)reader.GetInt32(reader.GetOrdinal("EntryType")); // convert the stored int value of the enum back to its enum type
            entry.Date = DateOnly.Parse(reader.GetString(reader.GetOrdinal("EntryDate")));
            entry.Notes = reader.GetString(reader.GetOrdinal("Notes"));

            return entry;
        }
    }
}