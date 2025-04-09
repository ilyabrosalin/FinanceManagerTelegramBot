using FinanceManagerTelegramBot.Models;
using Microsoft.Data.Sqlite;

namespace FinanceManagerTelegramBot.Services;

public class DatabaseService
{
    private const string ConnectionString = "Data Source=finance_bot.db";
    
    public DatabaseService()
    {
        InitializeDatabase();
    }
    
    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Создание таблицы Transactions
            var createTransactionsCommand = connection.CreateCommand();
            createTransactionsCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS Transactions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    Amount REAL NOT NULL,
                    Type TEXT NOT NULL CHECK(Type IN ('income', 'expense')),
                    Description TEXT,
                    Date TEXT NOT NULL
                )";
            createTransactionsCommand.ExecuteNonQuery();

            // Создание таблицы UserBalances
            var createBalancesCommand = connection.CreateCommand();
            createBalancesCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS UserBalances (
                    UserId INTEGER PRIMARY KEY,
                    CurrentBalance REAL NOT NULL DEFAULT 0.0
                )";
            createBalancesCommand.ExecuteNonQuery();

            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw new ApplicationException("Database initialization failed", ex);
        }
    }

    public void AddTransaction(Transaction transaction)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
    
        var command = connection.CreateCommand();
        command.CommandText = @"
        INSERT INTO Transactions (user_id, amount, type, description, date) 
        VALUES (@userId, @amount, @type, @description, @date)";
    
        command.Parameters.AddWithValue("@userId", transaction.UserId);
        command.Parameters.AddWithValue("@amount", transaction.Amount);
        command.Parameters.AddWithValue("@type", transaction.Type);
        command.Parameters.AddWithValue("@description", transaction.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@date", transaction.Date);
    
        command.ExecuteNonQuery();
    }

    // Основные методы для суммы 
    public decimal GetUserIncome(long userId, TimeSpan timeSpan)
        => GetFinancialAggregate(userId, timeSpan, "income", "SUM");

    public decimal GetUserExpenses(long userId, TimeSpan timeSpan)
        => GetFinancialAggregate(userId, timeSpan, "expense", "SUM");
    
    // Основные методы для средних значений
    public decimal GetAverageIncome(long userId, TimeSpan timeSpan)
        => GetFinancialAggregate(userId, timeSpan, "income", "AVG");

    public decimal GetAverageExpense(long userId, TimeSpan timeSpan)
        => GetFinancialAggregate(userId, timeSpan, "expense", "AVG");

    // Общий хелпер-метод
    private decimal GetFinancialAggregate(
        long userId, 
        TimeSpan timeSpan,
        string transactionType,
        string aggregateFunction)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = $@"
        SELECT COALESCE({aggregateFunction}(amount), 0)
        FROM Transactions 
        WHERE user_id = @userId 
        AND type = @transactionType
        AND date >= @timeLimit";

        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@timeLimit", DateTime.UtcNow - timeSpan);
        command.Parameters.AddWithValue("@transactionType", transactionType);

        return Convert.ToDecimal(command.ExecuteScalar());
    }
    
}