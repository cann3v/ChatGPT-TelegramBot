using System.Data.SQLite;
using log4net;

namespace chatgpt_bot;

public class Database
{
    private readonly string _filepath;
    private static readonly ILog Log = LogManager.GetLogger(typeof(Bot));
    private readonly SQLiteConnection _connection;
    
    public Database(string filepath)
    {
        _filepath = filepath;
        _connection = new SQLiteConnection($"Data Source={filepath};Version=3;");
    }

    public void CreateDatabaseFile()
    {
        if (!File.Exists(_filepath))
        {
            SQLiteConnection.CreateFile(_filepath);
            Log.Debug("Database file created.");
        }
        else
        {
            Log.Debug("Database file already exists.");
        }
    }

    public async Task CreateTables()
    {
        _connection.Open();
        Log.Debug($"Database connection state: {_connection.State}.");
        string createTableQuery = "CREATE TABLE IF NOT EXISTS Users (Id INTEGER PRIMARY KEY AUTOINCREMENT," +
                                  "User_id INTEGER, Username TEXT, First_use TEXT);" +
                                  "CREATE TABLE Chats (Id INTEGER PRIMARY KEY AUTOINCREMENT, User_id INTEGER," +
                                  "Data TEXT, FOREIGN KEY (User_id) REFERENCES Users (User_id));";
        await using (SQLiteCommand createTableCommand = new SQLiteCommand(createTableQuery, _connection))
        {
            int result = await createTableCommand.ExecuteNonQueryAsync();
            Log.Debug($"CreateTable result: {result}");
        }
        _connection.Close();
        Log.Debug($"Database connection state: {_connection.State}.");
    }
    
    public async Task InsertUsers(long user_id, string username)
    {
        _connection.Open();
        Log.Debug($"Database connection state: {_connection.State}.");
        string insertDataQuery = $"INSERT INTO Users (User_id, Username, First_use) SELECT '{user_id}', '{username}'," +
                          $"'{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' WHERE NOT EXISTS" +
                          $"(SELECT 1 FROM Users WHERE User_id = '{user_id}');";

        await using (SQLiteCommand insertDataCommand = new SQLiteCommand(insertDataQuery, _connection))
        {
            await insertDataCommand.ExecuteNonQueryAsync();
        }
        _connection.Close();
        Log.Debug($"Database connection state: {_connection.State}.");
    }

    public async Task InsertChats(long userId, string data)
    {
        bool chatExists = false;
        _connection.Open();
        Log.Debug($"Database connection state: {_connection.State}");

        string selectDataQuery = "SELECT 1 FROM Chats WHERE (User_id == @User_id)";
        await using (SQLiteCommand selectDataCommand = new SQLiteCommand(selectDataQuery, _connection))
        {
            selectDataCommand.Parameters.AddWithValue("@User_id", userId);
            var reader = await selectDataCommand.ExecuteScalarAsync();
            if (reader != null)
            {
                var result = reader.ToString();
                if (result == "1")
                    chatExists = true;
            }
        }

        if (!chatExists)
        {
            string insertDataQuery = "INSERT INTO Chats(User_id, Data) VALUES (@User_id, @Data)";
            await using (SQLiteCommand insertDataCommand = new SQLiteCommand(insertDataQuery, _connection))
            {
                insertDataCommand.Parameters.AddWithValue("@User_id", userId);
                insertDataCommand.Parameters.AddWithValue("@Data", data);
                int rows = await insertDataCommand.ExecuteNonQueryAsync();
                Log.Debug($"New chat created for user {userId} ({rows} rows affected)");
            }
        }
        else
        {
            string updateDataQuery = "UPDATE Chats SET Data = @Data WHERE User_id == @User_id";
            await using (SQLiteCommand updateDataCommand = new SQLiteCommand(updateDataQuery, _connection))
            {
                updateDataCommand.Parameters.AddWithValue("@Data", data);
                updateDataCommand.Parameters.AddWithValue("@User_id", userId);
                int rows = await updateDataCommand.ExecuteNonQueryAsync();
                Log.Debug($"Chat was updated for user {userId} ({rows} rows affected)");
            }
        }
        _connection.Close();
        Log.Debug($"Databasae connection state: {_connection.State}");
    }

    public async Task<string?> SelectChats(long userId)
    {
        string? chatData = null;
        _connection.Open();
        Log.Debug($"Database connection state: {_connection.State}");
        string selectDataQuery = "SELECT Data FROM Chats WHERE User_id = @User_id";
        await using (SQLiteCommand selectDataCommand = new SQLiteCommand(selectDataQuery, _connection))
        {
            selectDataCommand.Parameters.AddWithValue("@User_id", userId);
            var reader = await selectDataCommand.ExecuteScalarAsync();
            if (reader != null)
                chatData = reader.ToString();
        }
        _connection.Close();
        Log.Debug($"Database connection state: {_connection.State}");
        return chatData;
    }

    public async Task ClearChats(long userId)
    {
        _connection.Open();
        Log.Debug($"Database connection state: {_connection.State}");
        string deleteDataQuery = "DELETE FROM Chats WHERE User_id = @User_id";
        await using (SQLiteCommand deleteDataCommand = new SQLiteCommand(deleteDataQuery, _connection))
        {
            deleteDataCommand.Parameters.AddWithValue("@User_id", userId);
            int rows = await deleteDataCommand.ExecuteNonQueryAsync();
            Log.Debug($"Chat was deleted for user {userId} ({rows} rows affected)");
        }
        _connection.Close();
        Log.Debug($"Database connection state: {_connection.State}");
    }
}
