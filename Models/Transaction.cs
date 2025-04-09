namespace FinanceManagerTelegramBot.Models;

public class Transaction
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } // "income" или "expense"
    public string Description { get; set; }
    public DateTime Date { get; set; }
}