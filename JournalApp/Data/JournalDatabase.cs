using JournalApp.Models;
using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Xml.Linq;

namespace JournalApp.Data
{
    public class JournalDatabase
    {
        public event Action<JournalTreeItem>? TreeItemCreated;
        public event Action<JournalTreeItem, int?>? TreeItemEdited;
        public event Action<JournalTreeItem>? TreeItemDeleted;
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
            using SqliteCommand command = _connection.CreateCommand();
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

        private bool SaveEntry(JournalEntry journalEntry)
        {
            using SqliteConnection _connection = 
                new SqliteConnection($"Data Source={_databasePath}"); // creates db if it doesn't exist based source provided

            _connection.Open();
            using SqliteTransaction transaction = _connection.BeginTransaction();
            try
            {
                using SqliteCommand command = _connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText =
                    """
                INSERT INTO JournalEntries (Title, EntryType, FolderId, EntryDate, Notes)
                VALUES($title, $entryType, $folderId, $entryDate, $notes)
                RETURNING Id;
                """;
                command.Parameters.AddWithValue("$title", journalEntry.Name);
                command.Parameters.AddWithValue("$entryType", (int)journalEntry.EType);
                if (journalEntry.FolderId == null)
                {
                    command.Parameters.AddWithValue("$folderId", DBNull.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("$folderId", journalEntry.FolderId);
                }
                command.Parameters.AddWithValue("$entryDate", journalEntry.Date.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("$notes", journalEntry.Notes);
                object? result = command.ExecuteScalar();
                if (result == null)
                {
                    transaction.Rollback();
                    return false;
                }
                journalEntry.Id = Convert.ToInt32(result);
                transaction.Commit();
                return true;
            } catch (Exception ex)
            {
                Debug.WriteLine($"Issue saving entry in DB: {ex.Message}");
                transaction.Rollback();
                return false;
            }
        }

        private bool EditEntry(JournalEntry journalEntry)
        {
            using SqliteConnection _connection =
                new SqliteConnection($"Data Source={_databasePath}"); // creates db if it doesn't exist based source provided

            _connection.Open();
            using SqliteTransaction transaction = _connection.BeginTransaction();
            try
            {
                using SqliteCommand command = _connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText =
                    """
                    UPDATE JournalEntries
                    SET
                        Title = $title,
                        EntryType = $entryType,
                        EntryDate = $entryDate,
                        Notes = $notes,
                        FolderId = $folderId
                    WHERE Id = $id;
                    """;
                command.Parameters.AddWithValue("$title", journalEntry.Name);
                command.Parameters.AddWithValue("$entryType", (int)journalEntry.EType);
                command.Parameters.AddWithValue("$id", journalEntry.Id);
                if (journalEntry.FolderId == null)
                {
                    command.Parameters.AddWithValue("$folderId", DBNull.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("$folderId", journalEntry.FolderId);
                }
                command.Parameters.AddWithValue("$entryDate", journalEntry.Date.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("$notes", journalEntry.Notes);

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected != 1)
                {
                    transaction.Rollback();
                    return false;
                }
                transaction.Commit();
                return true;
            } catch (Exception ex)
            {
                Debug.WriteLine($"Issue editing entry from DB: {ex.Message}");
                transaction.Rollback();
                return false;
            }
        }
        public (bool, List<JournalEntry>) GetAllEntries()
        {
            using SqliteConnection connection =
                new SqliteConnection($"Data Source={_databasePath}");

            connection.Open();
            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
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
                transaction.Commit();
                return (true, entries);
            } catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine($"Issue getting all entries from DB: {ex.Message}");
                List<JournalEntry> empty = new();
                return (false, empty);
            }
        }
        private bool DeleteEntry(int id)
        {
            using SqliteConnection connection =
                new SqliteConnection($"Data Source={_databasePath}");
            connection.Open();
            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                using SqliteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText =
                    """
                    DELETE FROM JournalEntries
                    WHERE Id = $id;
                    """;
                command.Parameters.AddWithValue("$id", id);
                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected != 1)
                {
                    transaction.Rollback();
                    return false;
                }
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"issue deleting entry from DB: {ex.Message}");
                transaction.Rollback();
                return false;
            }
        }

        public bool DeleteJournalTreeItem(JournalTreeItem journalTreeItem)
        {
            bool success;

            if (journalTreeItem is JournalEntry entry)
            {
                success = DeleteEntry(entry.Id);
            } else if (journalTreeItem is JournalFolder folder)
            {
                success = DeleteFolder(folder.Id);
            } else
            {
                return false;
            }
            if (success)
            {
                TreeItemDeleted?.Invoke(journalTreeItem);
            }
            return success;
        }

        public bool EditJournalTreeItem(JournalTreeItem journalTreeItem, int? oldParentId)
        {
            if (journalTreeItem is JournalEntry entry)
            {
                bool success = EditEntry(entry);
                if (success)
                {
                    TreeItemEdited?.Invoke(entry, oldParentId);
                }
                return success;
            }

            if (journalTreeItem is JournalFolder folder)
            {
                bool success = EditFolder(folder);
                if (success)
                {
                    TreeItemEdited?.Invoke(folder, oldParentId);
                }
                return success;
            }
            return false;
        }
        public bool CreateJournalTreeItem(JournalTreeItem journalTreeItem)
        {
            if (journalTreeItem is JournalEntry entry)
            {
                bool success = SaveEntry(entry);
                if (success)
                {
                    TreeItemCreated?.Invoke(entry);
                }
                return success;
            }

            if (journalTreeItem is JournalFolder folder)
            {
                bool success = CreateFolder(folder);
                if (success)
                {
                    TreeItemCreated?.Invoke(folder);
                }
                return success;
            }
            return false;
        }

        private bool CreateFolder(JournalFolder folder)
        {
            using SqliteConnection _connection = new SqliteConnection($"Data Source = {_databasePath}");
            _connection.Open();
            using SqliteTransaction transaction = _connection.BeginTransaction();
            try
            {
                using SqliteCommand command = _connection.CreateCommand();
                command.Transaction= transaction;
                command.CommandText =
                """
                INSERT INTO JournalFolders (Name, ParentFolderId)
                VALUES ($name, $parentFolderId)
                RETURNING Id;
                """;
                command.Parameters.AddWithValue("$name", folder.Name);
                if (folder.ParentFolderId != null)
                {
                    command.Parameters.AddWithValue("$parentFolderId", folder.ParentFolderId);
                }
                else
                {
                    command.Parameters.AddWithValue("$parentFolderId", DBNull.Value);
                }

                object? result = command.ExecuteScalar();
                if (result == null)
                {
                    transaction.Rollback();
                    return false;
                }
                folder.Id = Convert.ToInt32(result);

                transaction.Commit();
                return true;
            } catch (Exception ex)
            {
                Debug.WriteLine($"Issue creating folder in DB: {ex.Message}");
                transaction.Rollback();
                return false;
            }
        }

        private bool EditFolder(JournalFolder folder)
        {
            using SqliteConnection _connection = new SqliteConnection($"Data Source = {_databasePath}");
            _connection.Open();
            using SqliteTransaction transaction = _connection.BeginTransaction();
            try
            {
                using SqliteCommand command = _connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText =
                    """
                    UPDATE JournalFolders
                    SET
                        Name = $name,
                        ParentFolderId = $parentFolderId
                    WHERE Id = $id;
                    """;
                command.Parameters.AddWithValue("$name", folder.Name);
                if (folder.ParentFolderId != null)
                {
                    command.Parameters.AddWithValue("$parentFolderId", folder.ParentFolderId);
                }
                else
                {
                    command.Parameters.AddWithValue("$parentFolderId", DBNull.Value);
                }
                command.Parameters.AddWithValue("$id", folder.Id);
                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected != 1)
                {
                    transaction.Rollback();
                    return false;
                }

                transaction.Commit();
                return true;
            } catch (Exception ex)
            {
                Debug.WriteLine($"Issue editing folder in DB: {ex.Message}");
                transaction.Rollback();
                return false;
            }
        }

        private bool DeleteFolder(int id)
        {
            using SqliteConnection connection =
                new SqliteConnection($"Data Source={_databasePath}");

            connection.Open();
            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                DeleteFolderRecursive(id, connection, transaction);
                transaction.Commit();
                return true;
            } catch (Exception ex)
            {
                Debug.WriteLine($"Issue deleting folder in DB: {ex.Message}");
                transaction.Rollback();
                return false;
            }
        }

        private void DeleteFolderRecursive(int folderId, SqliteConnection connection, SqliteTransaction transaction)
        {
            // Delete entries directly inside this folder
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    """
            DELETE FROM JournalEntries
            WHERE FolderId = $folderId;
            """;

                command.Parameters.AddWithValue("$folderId", folderId);
                command.ExecuteNonQuery();
            }

            // Get child folders
            List<int> childFolderIds = new();

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    """
            SELECT Id
            FROM JournalFolders
            WHERE ParentFolderId = $folderId;
            """;

                command.Parameters.AddWithValue("$folderId", folderId);

                using SqliteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    childFolderIds.Add(reader.GetInt32(0));
                }
            }

            // Delete each child folder recursively
            foreach (int childFolderId in childFolderIds)
            {
                DeleteFolderRecursive(
                    childFolderId,
                    connection,
                    transaction);
            }

            // Delete this folder after its contents are gone
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    """
                    DELETE FROM JournalFolders
                    WHERE Id = $folderId;
                    """;

                command.Parameters.AddWithValue("$folderId", folderId);

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected != 1)
                {
                    throw new InvalidOperationException(
                        $"Expected to delete folder {folderId}, but deleted {rowsAffected} rows.");
                }
            }
        }

        public (bool, List<JournalFolder>) GetAllFolders()
        {
            using SqliteConnection _connection = new SqliteConnection($"Data Source = {_databasePath}");
            _connection.Open();
            using SqliteTransaction transaction = _connection.BeginTransaction();
            try
            {
                SqliteCommand command = _connection.CreateCommand();
                command.Transaction = transaction;
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
                transaction.Commit();
                return (true, journalFolders);
            } catch
            {
                transaction.Rollback();
                Debug.WriteLine("issue grabbing all folders from db");
                List<JournalFolder> empty = new();
                return (false, empty);
            }
        }

        public (ObservableCollection<JournalTreeItem>, Dictionary<int, JournalFolder>, Dictionary<int, JournalEntry>) BuildJournalTree(List<JournalTreeItem> tree)
        {
            ObservableCollection<JournalTreeItem> finalList = new ObservableCollection<JournalTreeItem>();
            Dictionary<int, JournalFolder> folderDict = new Dictionary<int, JournalFolder>();
            Dictionary<int, JournalEntry> entryDict = new Dictionary<int, JournalEntry>();

            foreach (JournalTreeItem treeItem in tree)
            {
                if (treeItem is JournalEntry entry)
                {
                    entryDict[entry.Id] = entry;
                    if (entry.FolderId == null)
                    {
                        finalList.Add(entry);
                    }
                } else if (treeItem is JournalFolder folder)
                {
                    if (folder.ParentFolderId == null)
                    {
                        finalList.Add(folder);
                    }
                    folder.Children.Clear();
                    folderDict[folder.Id] = folder;
                }
            }

            foreach (JournalTreeItem treeItem in tree)
            {
                if (treeItem is JournalEntry entry)
                {
                    if (entry.FolderId != null)
                    {
                        if (folderDict.TryGetValue((int)entry.FolderId, out JournalFolder? parentFolder))
                        {
                            parentFolder.Children.Add(entry);
                        }
                        else
                        {
                            Debug.WriteLine($"Folder: Name = {entry.Name}, Id = {entry.Id}, ParentId = {entry.FolderId} - entry is pointing to missing parent folder");
                        }
                    }
                } else if (treeItem is JournalFolder folder)
                {
                    if (folder.ParentFolderId != null)
                    {
                        if (folderDict.TryGetValue((int)folder.ParentFolderId, out JournalFolder? parentFolder))
                        {
                            parentFolder.Children.Add(folder);
                        }
                        else
                        {
                            Debug.WriteLine($"Folder: {folder.Name}, {folder.Id}, {folder.ParentFolderId} - Child folder with missing parent detected.Couldnt get value associate with parenfolderid passed in");
                        }
                    }
                }
            }
            return (finalList, folderDict, entryDict);
        }
        private JournalEntry ConvertToJournalEntry(SqliteDataReader reader)
        {

            JournalEntry entry = new JournalEntry();

            entry.Id = reader.GetInt32(reader.GetOrdinal("Id"));
            entry.Name = reader.GetString(reader.GetOrdinal("Title")); // get the column position and then get the string stored at the position explicitly 
            if (!reader.IsDBNull(reader.GetOrdinal("FolderId")))
            {
                entry.FolderId = reader.GetInt32(reader.GetOrdinal("FolderId"));
            }
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
                folder.ParentFolderId = reader.GetInt32(parentFolderOrdinal);
            }
            return folder;
        }

        private int GetSchemaVersion(SqliteConnection connection)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT Version
                FROM SchemaInfo
                LIMIT 1;
                """;

            object? result = command.ExecuteScalar();

            if (result == null)
            {
                throw new InvalidOperationException(
                    "Could not determine database schema version.");
            }

            return Convert.ToInt32(result);
        }

        private void MigrateToVersion2(SqliteConnection connection)
        {
            using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                using SqliteCommand command = connection.CreateCommand();
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