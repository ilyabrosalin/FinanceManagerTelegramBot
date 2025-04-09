using Telegram.Bot;

namespace FinanceManagerTelegramBot;

public class FinanceBot
{
    private readonly ITelegramBotClient botClient;
    private readonly ICommandHandler commandHandler;

    public FinanceBot(ITelegramBotClient botClient, ICommandHandler commandHandler)
    {
        this.botClient = botClient;
        this.commandHandler = commandHandler;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        botClient.StartReceiving(
            updateHandler: async (botClient, update, cancellationToken) =>
            {
                await commandHandler.HandleAsync(botClient, update, cancellationToken);
            },
            errorHandler: HandleErrorAsync
        );

        Console.WriteLine("Бот запущен.");
        await Task.Delay(-1, cancellationToken); // Ожидание для завершения работы
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }
}