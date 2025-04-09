using FinanceManagerTelegramBot.Handlers;
using Telegram.Bot;

namespace FinanceManagerTelegramBot;

class Program
{
    static async Task Main(string[] args)
    {
        var botToken = "8088319020:AAGISDH5bk_nj4rE-zrKURGNx091BUnNcU8";
        var botClient = new TelegramBotClient(botToken);
        var commandHandler = new StartCommandHandler(); // Инициализируем обработчик команд

        var bot = new FinanceBot(botClient, commandHandler);
        var cancellationTokenSource = new CancellationTokenSource();

        await bot.StartAsync(cancellationTokenSource.Token);
    }
}