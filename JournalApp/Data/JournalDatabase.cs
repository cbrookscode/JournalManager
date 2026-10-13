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

        public string CreateSnapshot()
        {
            string snapshotDirectory = Path.Combine(
                _appDataDirectory, "Snapshots");

            Directory.CreateDirectory(snapshotDirectory);

            string fileName =
                $"journal_{DateTime.Now:yyyyMMdd_HHmmss}.db";

            string snapshotPath = Path.Combine(snapshotDirectory, fileName);
            string tempPath = snapshotPath + ".tmp";

            try
            {
                // Step 1: Create backup in temporary file
                using (SqliteConnection source = new SqliteConnection(
                    new SqliteConnectionStringBuilder
                    {
                        DataSource = _databasePath,
                        Mode = SqliteOpenMode.ReadOnly,
                        Pooling = false
                    }.ToString()))
                using (SqliteConnection destination = new SqliteConnection(
                    new SqliteConnectionStringBuilder
                    {
                        DataSource = tempPath,
                        Pooling = false
                    }.ToString()))
                {
                    source.Open();
                    destination.Open();

                    source.BackupDatabase(destination);
                }

                // Step 2: Verify backup integrity
                using (SqliteConnection connection = new SqliteConnection(
                    new SqliteConnectionStringBuilder
                    {
                        DataSource = tempPath,
                        Mode = SqliteOpenMode.ReadOnly,
                        Pooling = false
                    }.ToString()))
                {
                    connection.Open();

                    using SqliteCommand command = connection.CreateCommand();
                    command.CommandText = "PRAGMA integrity_check;";

                    string? result = command.ExecuteScalar()?.ToString();

                    if (result != "ok")
                    {
                        throw new InvalidOperationException(
                            $"Snapshot integrity check failed: {result}");
                    }
                }

                // Step 3: Finalize snapshot
                File.Move(tempPath, snapshotPath);

                return snapshotPath;
            }
            catch
            {
                // Step 4: Remove incomplete snapshot
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch (IOException)
                {
                    // Preserve original exception
                }
                catch (UnauthorizedAccessException)
                {
                    // Preserve original exception
                }

                throw;
            }
        }

        public string ImportDatabase(string importPath)
        {
            // Step 1: Validate the imported database
            if (!ValidateDatabase(importPath))
            {
                throw new InvalidOperationException(
                    "Database validation failed.");
            }

            // Prevent importing the currently active database
            if (Path.GetFullPath(importPath).Equals(
                Path.GetFullPath(_databasePath),
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Cannot import the currently active database.");
            }

            // Step 2: Snapshot the existing database
            string backupPath = CreateSnapshot();

            // Step 3: Import the new database
            using (SqliteConnection source = new SqliteConnection(
                new SqliteConnectionStringBuilder
                {
                    DataSource = importPath,
                    Mode = SqliteOpenMode.ReadOnly,
                    Pooling = false
                }.ToString()))
            using (SqliteConnection destination = new SqliteConnection(
                new SqliteConnectionStringBuilder
                {
                    DataSource = _databasePath,
                    Mode = SqliteOpenMode.ReadWrite,
                    Pooling = false
                }.ToString()))
            {
                source.Open();
                destination.Open();

                source.BackupDatabase(destination);
            }

            return backupPath;
        }

        public bool ValidateDatabase(string databasePath)
        {
            if (!File.Exists(databasePath))
            {
                throw new FileNotFoundException(
                    "The selected database file does not exist.",
                    databasePath);
            }

            using SqliteConnection connection = new SqliteConnection(
                new SqliteConnectionStringBuilder
                {
                    DataSource = databasePath,
                    Mode = SqliteOpenMode.ReadOnly,
                    Pooling = false
                }.ToString());

            connection.Open();

            // Step 1: Check database integrity
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA integrity_check;";

                using SqliteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    if (reader.GetString(0) != "ok")
                    {
                        throw new InvalidOperationException(
                            "The selected database failed its integrity check.");
                    }
                }
            }

            // Step 2: Validate required tables and columns
            Dictionary<string, string[]> requiredSchema = new()
            {
                {
                    "JournalEntries",
                    new[]
                    {
                        "Id",
                        "Title",
                        "EntryType",
                        "EntryDate",
                        "Notes",
                        "FolderId"
                    }
                },
                {
                    "JournalFolders",
                    new[]
                    {
                        "Id",
                        "Name",
                        "ParentFolderId"
                    }
                }
            };

            foreach (var table in requiredSchema)
            {
                HashSet<string> existingColumns = new(
                    StringComparer.OrdinalIgnoreCase);

                using SqliteCommand command = connection.CreateCommand();

                command.CommandText =
                    $"PRAGMA table_info(\"{table.Key}\");";

                using SqliteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    existingColumns.Add(reader.GetString(1));
                }

                foreach (string requiredColumn in table.Value)
                {
                    if (!existingColumns.Contains(requiredColumn))
                    {
                        throw new InvalidOperationException(
                            $"Database validation failed. " +
                            $"Table '{table.Key}' is missing column '{requiredColumn}'.");
                    }
                }
            }

            // Step 3: Check schema version
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA user_version;";

                int databaseVersion =
                    Convert.ToInt32(command.ExecuteScalar());

                // Replace with your application's current schema version
                int currentVersion = 2;

                if (databaseVersion > currentVersion)
                {
                    throw new InvalidOperationException(
                        $"The database version ({databaseVersion}) is newer " +
                        $"than the application supports ({currentVersion}).");
                }
            }

            return true;
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

        public (bool, JournalFolder) GetFolderById(int? id)
        {
            if (id == null) return (false, new JournalFolder());

            using SqliteConnection _connection = new SqliteConnection($"Data Source = {_databasePath}");
            _connection.Open();
            using SqliteTransaction transaction = _connection.BeginTransaction();
            try
            {
                SqliteCommand command = _connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText =
                """
                SELECT FROM JournalFolders
                WHERE Id = $id;
                """;
                command.Parameters.AddWithValue("$id", id);
                using SqliteDataReader reader = command.ExecuteReader();

                List<JournalFolder> journalFolders = new List<JournalFolder>();
                reader.Read();

                JournalFolder folder = ConverToJournalFolder(reader);
                transaction.Commit();
                return (true, folder);
            }
            catch
            {
                transaction.Rollback();
                Debug.WriteLine("issue grabbing folder via provided from db");
                JournalFolder empty = new();
                return (false, empty);
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