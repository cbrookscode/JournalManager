using JournalApp.Models;
using Microsoft.Data.Sqlite;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;

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
                CREATE TABLE IF NOT EXISTS JournalFolders (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL,
                    ParentFolderId INTEGER NULL
                );
                CREATE TABLE IF NOT EXISTS SchemaInfo (
                    Version INTEGER NOT NULL
                );
                INSERT INTO SchemaInfo (Version)
                SELECT 1
                WHERE NOT EXISTS (
                    SELECT 1 FROM SchemaInfo
                );
                """;
            command.ExecuteNonQuery();

            int version = GetSchemaVersion(_connection);
            if (version < 2)
            {
                MigrateToVersion2(_connection);
                version = 2;
            }
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
        public void DeleteEntry(int id)
        {
            using SqliteConnection connection =
                new SqliteConnection($"Data Source={_databasePath}");

            connection.Open();
            SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                DELETE FROM JournalEntries
                WHERE Id = $id;
                """;
            command.Parameters.AddWithValue("$id", id);
            using SqliteDataReader reader = command.ExecuteReader();
        }

        public void CreateFolder(JournalFolder folder)
        {
            using SqliteConnection _connection = new SqliteConnection($"Data Source = {_databasePath}");
            _connection.Open();
            SqliteCommand command = _connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO JournalFolders (Name, ParentFolderId)
                VALUES ($name, $parentFolderId);
                """;
            command.Parameters.AddWithValue("$name", folder.Name );
            if (folder.ParentFolderId != null)
            {
                command.Parameters.AddWithValue("$parentFolderId", folder.ParentFolderId);
            } else
            {
                command.Parameters.AddWithValue("$parentFolderId", DBNull.Value);
            }
            command.ExecuteNonQuery();
            _connection.Close();
        }

        public void DeleteFolder(int id)
        {
            using SqliteConnection connection =
                new SqliteConnection($"Data Source={_databasePath}");

            connection.Open();
            SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                DELETE FROM JournalFolders
                WHERE Id = $id;
                """;
            command.Parameters.AddWithValue("$id", id);
            using SqliteDataReader reader = command.ExecuteReader();
        }

        public List<JournalFolder> GetAllFolders()
        {
            using SqliteConnection _connection = new SqliteConnection($"Data Source = {_databasePath}");
            _connection.Open();
            SqliteCommand command = _connection.CreateCommand();
            command.CommandText =
                """
                SELECT * FROM JournalFolders;
                """;
            using SqliteDataReader reader = command.ExecuteReader();

            List<JournalFolder> journalFolders = new List<JournalFolder>();
            while (reader.Read())
            {
                JournalFolder folder = ConverToJournalFolder(reader);
                journalFolders.Add(folder);
            }
            return journalFolders;
        }

        public (ObservableCollection<JournalFolder>, Dictionary<int, JournalFolder>) SetupChildrenFolders(List<JournalFolder> folders)
        {
            ObservableCollection<JournalFolder> finalList = new ObservableCollection<JournalFolder>();
            Dictionary<int, JournalFolder> folderDict = new Dictionary<int, JournalFolder>();

            foreach (JournalFolder folder in folders)
            {
                if (folder.ParentFolderId == null)
                {
                    finalList.Add(folder);
                }
                folder.Children.Clear();
                folderDict[folder.Id] = folder;
            }

            foreach (JournalFolder folder in folders)
            {
                if (folder.ParentFolderId != null)
                {
                    if (folderDict.TryGetValue((int) folder.ParentFolderId, out JournalFolder? parentFolder)) 
                    {
                        parentFolder.Children.Add(folder);
                    } else
                    {
                        Debug.WriteLine("Couldnt get value associate with parenfolderid passed in");
                    }
                }
            }
            return (finalList, folderDict);
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

        private JournalFolder ConverToJournalFolder(SqliteDataReader reader)
        {
            JournalFolder folder = new JournalFolder();
            folder.Id = reader.GetInt32(reader.GetOrdinal("Id"));
            folder.Name = reader.GetString(reader.GetOrdinal("Name"));
            int parentFolderOrdinal = reader.GetOrdinal("ParentFolderId");
            if (reader.IsDBNull(parentFolderOrdinal)) 
            {
                folder.ParentFolderId = null;
            } else
            {
                folder.ParentFolderId = parentFolderOrdinal;
            }
            return folder;
        }

        private int GetSchemaVersion(SqliteConnection connection)
        {
            SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT Version
                FROM SchemaInfo
                LIMIT 1;
                """;

            int version = Convert.ToInt32(command.ExecuteScalar());
            return version;
        }

        private void MigrateToVersion2(SqliteConnection connection)
        {
            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText =
                        """
                    ALTER TABLE JournalEntries
                    ADD COLUMN FolderId INTEGER NULL;

                    UPDATE SchemaInfo
                    SET Version = 2;
                    """;

                command.ExecuteNonQuery();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}