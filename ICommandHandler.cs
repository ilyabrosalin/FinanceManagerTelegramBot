using Telegram.Bot;
using Telegram.Bot.Types;

namespace FinanceManagerTelegramBot;

public interface ICommandHandler
{
    Task HandleAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken);
}