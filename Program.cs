using System.Text.RegularExpressions;
using finance_manager_telegram_bot;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

// Константы для конфигурации
const string botToken = "8088319020:AAGISDH5bk_nj4rE-zrKURGNx091BUnNcU8";
const string numberPattern = @"^\s*[1-9]\d*(?:[.,]\d+)?\s*$";
const string errorMessage = 
    "❌ *Неверный формат данных*\n\n" +
    "Я принимаю только:\n" +
    "• Положительные числа (например: 10, 25.5, 1000)\n" +
    "• Команду /stats для получения статистики\n\n" +
    "Пожалуйста, попробуйте еще раз или введите /stats для просмотра вашей статистики.";

// Инициализация сервисов
var botClient = new TelegramBotClient(botToken);
var dbService = new DatabaseService();
var numRegex = new Regex(numberPattern, RegexOptions.Compiled);
    
// Запуск обработки сообщений
botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    errorHandler: HandleErrorAsync
);

Console.ReadLine();
return;

async Task HandleUpdateAsync(
    ITelegramBotClient botClient, 
    Update update, 
    CancellationToken cancellationToken)
{
    try
    {
        switch (update)
        {
            case { Message: { } message }:
                await HandleMessageAsync(botClient, message, cancellationToken);
                break;
                
            case { CallbackQuery: { } callbackQuery }:
                await HandleCallbackQueryAsync(botClient, callbackQuery, cancellationToken);
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при обработке обновления: {ex.Message}");
    }
}

async Task HandleMessageAsync(
    ITelegramBotClient botClient,
    Message message,
    CancellationToken cancellationToken)
{
    if (message.From is not { } user)
        return;

    var userId = user.Id;
    var messageText = message.Text ?? string.Empty;

    // Логирование входящего сообщения
    LogMessage(user.Username, userId, messageText);
    
    // Обработка команды /start
    if (messageText.Equals("/start", StringComparison.OrdinalIgnoreCase))
    {
        await SendWelcomeMessage(botClient, message.Chat.Id, cancellationToken);
        return;
    }
    
    // Обработка числового ввода
    if (numRegex.IsMatch(messageText))
    {
        dbService.SaveMessage(userId, messageText.Trim());
        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "✅ Число успешно сохранено!\nИспользуйте /stats для просмотра статистики",
            cancellationToken: cancellationToken);
    }
    else if (messageText.StartsWith("/stats"))
    {
        await SendPeriodSelectionMenu(botClient, message.Chat.Id, cancellationToken);
    }
    else
    {
        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: errorMessage,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }
}

async Task HandleCallbackQueryAsync(
    ITelegramBotClient botClient,
    CallbackQuery callbackQuery,
    CancellationToken cancellationToken)
{
    try
    {
        await botClient.AnswerCallbackQuery(callbackQuery.Id);
        var message = callbackQuery.Message!;

        // Анимация загрузки с точками
        for (int i = 1; i <= 3; i++)
        {
            var dots = new string('.', i);
            await botClient.EditMessageText(
                chatId: message.Chat.Id,
                messageId: message.MessageId,
                text: $"⌛ Загрузка{dots}",
                replyMarkup: null, // Убираем все кнопки
                cancellationToken: cancellationToken);
            
            await Task.Delay(500, cancellationToken);
        }

        // Получение данных
        var (timeSpan, periodText) = callbackQuery.Data switch
        {
            "summary_week" => (TimeSpan.FromDays(7), "неделю"),
            "summary_month" => (TimeSpan.FromDays(30), "месяц"),
            _ => (TimeSpan.Zero, "")
        };

        var userId = callbackQuery.From.Id;
        var total = dbService.GetUserTotalAmount(userId, timeSpan);
        var average = dbService.GetUserAverageAmount(userId, timeSpan);

        // Формирование финального ответа
        var response = total > 0
            ? $"📊 *Статистика за {periodText}:*\n" +
              $"• Общая сумма: {total:C}\n" +
              $"• Средняя сумма: {average:C}"
            : $"❌ Данных за {periodText} нет";

        await botClient.EditMessageText(
            chatId: message.Chat.Id,
            messageId: message.MessageId,
            text: response,
            parseMode: ParseMode.Markdown,
            replyMarkup: null, // Убираем кнопки
            cancellationToken: cancellationToken);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
        await botClient.SendMessage(
            chatId: callbackQuery.Message!.Chat.Id,
            text: "⚠️ Ошибка обработки запроса",
            cancellationToken: cancellationToken);
    }
}

async Task SendPeriodSelectionMenu(
    ITelegramBotClient botClient,
    long chatId,
    CancellationToken cancellationToken)
{
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("📅 Итоги за неделю", "summary_week"),
            InlineKeyboardButton.WithCallbackData("📆 Итоги за месяц", "summary_month")
        }
    });

    await botClient.SendMessage(
        chatId: chatId,
        text: "Выберите период, за который хотите получить отчет:",
        replyMarkup: inlineKeyboard,
        cancellationToken: cancellationToken);
}

void LogMessage(string username, long userId, string messageText)
{
    Console.WriteLine($"Получено сообщение от @{username} (ID: {userId}):");
    Console.WriteLine($"Текст: {messageText}");
    Console.WriteLine(new string('-', 30));
}

async Task SendWelcomeMessage(
    ITelegramBotClient botClient,
    long chatId,
    CancellationToken cancellationToken)
{
    const string welcomeText = 
        "👋 *Добро пожаловать в Finance Tracker Bot!*\n\n" +
        "📈 *Основные функции:*\n" +
        "1. Сохранение финансовых операций\n" +
        "   Просто отправьте число (например: _150_ или _75.5_)\n\n" +
        "2. Просмотр статистики\n" +
        "   Используйте команду /stats\n\n" +
        "3. Анализ за период\n" +
        "   Выбирайте неделю или месяц для отчетов\n\n" +
        "📌 *Примеры использования:*\n" +
        "• _100_ - сохранить расход 100 рублей\n" +
        "• _/stats_ - показать меню статистики";

    await botClient.SendMessage(
        chatId: chatId,
        text: welcomeText,
        parseMode: ParseMode.Markdown,
        cancellationToken: cancellationToken);
}

async Task HandleErrorAsync(
    ITelegramBotClient botClient, 
    Exception exception, 
    CancellationToken cancellationToken)
{
    Console.WriteLine($"Ошибка: {exception.Message}");
}