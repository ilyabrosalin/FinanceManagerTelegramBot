using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FinanceManagerTelegramBot.Handlers;

public class StartCommandHandler : ICommandHandler
{
    public async Task HandleAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message?.Text.Equals("/start", StringComparison.OrdinalIgnoreCase) == true)
        {
            await SendWelcomeMessage(botClient, update.Message.Chat.Id, cancellationToken);
        }
    }

    private async Task SendWelcomeMessage(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        const string welcomeText = 
            "👋 *Добро пожаловать в Finance Tracker Bot!*\n\n" +
            "Я помогу вам отслеживать ваши финансовые операции.";

        await botClient.SendMessage(
            chatId: chatId,
            text: welcomeText,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }
}