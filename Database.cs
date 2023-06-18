using System.Data.Entity;
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
            _connection.Open();
            Log.Debug($"Database connection state: {_connection.State}.");
            SQLiteConnection.CreateFile(_filepath);
            _connection.Close();
            Log.Debug($"Database connection state: {_connection.State}");
            Log.Debug("Database file created.");
        }
        else
        {
            Log.Debug("Database file already exists.");
        }
    }

    public async void CreateTable()
    {
        _connection.Open();
        Log.Debug($"Database connection state: {_connection.State}.");
        string createTableQuery = "CREATE TABLE IF NOT EXISTS Users (Id INTEGER PRIMARY KEY AUTOINCREMENT," +
                                  "User_id INTEGER, Username TEXT, First_use TEXT);";
        await using (SQLiteCommand createTableCommand = new SQLiteCommand(createTableQuery, _connection))
        {
            int result = await createTableCommand.ExecuteNonQueryAsync();
            Log.Debug($"CreateTable result: {result}");
        }
        _connection.Close();
        Log.Debug($"Database connection state: {_connection.State}.");
    }
    
    public async void InsertData(long user_id, string username)
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
}
