using Microsoft.Data.Sqlite;

namespace finance_manager_telegram_bot;

public class DatabaseService
{
    private const string ConnectionString = "Data Source=messages.db";
    
    public DatabaseService()
    {
        InitializeDatabase();
    }
    
    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Messages (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    user_id INTEGER NOT NULL,
                    amount TEXT NOT NULL,
                    date TEXT DEFAULT (strftime('%Y-%m-%d %H:%M:%S', 'now'))
                )";
        command.ExecuteNonQuery();
    }

    public void SaveMessage(long userId, string message)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
                INSERT INTO Messages (user_id, amount)
                VALUES (@userId, @message)";
            
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@message", message);
            
        command.ExecuteNonQuery();
    }

    public decimal GetUserTotalAmount(long userId, TimeSpan timeSpan)
    {
        decimal totalAmount = 0;

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT SUM(CAST(amount AS REAL)) 
            FROM Messages 
            WHERE user_id = @userId 
            AND date >= @timeLimit";
        
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@timeLimit", DateTime.UtcNow - timeSpan);

        var result = command.ExecuteScalar();
        if (result != DBNull.Value && result != null)
        {
            totalAmount = Convert.ToDecimal(result);
        }

        return totalAmount;
    }
    
    public decimal GetUserAverageAmount(long userId, TimeSpan timeSpan)
    {
        decimal averageAmount = 0;

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT AVG(CAST(amount AS REAL)) 
            FROM Messages 
            WHERE user_id = @userId 
            AND date >= @timeLimit";
        
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@timeLimit", DateTime.UtcNow - timeSpan);

        var result = command.ExecuteScalar();
        if (result != DBNull.Value && result != null)
        {
            averageAmount = Convert.ToDecimal(result);
        }

        return averageAmount;
    }
}