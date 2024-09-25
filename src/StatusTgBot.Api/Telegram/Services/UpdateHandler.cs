using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json.Linq;
using MediatR;
using StatusTgBot.Api.Infrastructure.Commands;
using StatusTgBot.Api.Data.Migrations;
using System.Text.RegularExpressions;

namespace StatusTgBot.Api.Telegram.Services
{
    public class UpdateHandler(ITelegramBotClient bot, IServiceProvider services, ILogger<UpdateHandler> logger) : IUpdateHandler
    {
        private static readonly InputPollOption[] PollOptions = ["Hello", "World!"];

        public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            logger.LogInformation("HandleError: {Exception}", exception);
            // Cooldown in case of network connection error
            if (exception is RequestException)
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await (update switch
            {
                { Message: { } message } => OnMessage(message),
                { EditedMessage: { } message } => OnMessage(message),
                { CallbackQuery: { } callbackQuery } => OnCallbackQuery(callbackQuery),
                { InlineQuery: { } inlineQuery } => OnInlineQuery(inlineQuery),
                { ChosenInlineResult: { } chosenInlineResult } => OnChosenInlineResult(chosenInlineResult),
                { Poll: { } poll } => OnPoll(poll),
                { PollAnswer: { } pollAnswer } => OnPollAnswer(pollAnswer),
                // UpdateType.ChannelPost:
                // UpdateType.EditedChannelPost:
                // UpdateType.ShippingQuery:
                // UpdateType.PreCheckoutQuery:
                _ => UnknownUpdateHandlerAsync(update)
            });
        }

        private async Task OnMessage(Message msg)
        {
            logger.LogInformation("Receive message type: {MessageType}", msg.Type);

            using var scope = services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();


            if (msg.Text is not { } messageText)
                return;

            if (messageText.StartsWith("/"))
            {

                Message sentMessage = await (messageText.Split(' ')[0] switch
                {
                    "/photo" => SendPhoto(msg),
                    "/inline_buttons" => SendInlineKeyboard(msg),
                    "/keyboard" => SendReplyKeyboard(msg),
                    "/remove" => RemoveKeyboard(msg),
                    "/request" => RequestContactAndLocation(msg),
                    "/inline_mode" => StartInlineQuery(msg),
                    "/poll" => SendPoll(msg),
                    "/poll_anonymous" => SendAnonymousPoll(msg),
                    "/throw" => FailingHandler(msg),
                    "/start" => Usage(msg)
                });
            }
            else
            {
                if(msg.Chat.Id == 619367092 && msg.ReplyToMessage is not null)
                {
                    var originalMsg = msg.ReplyToMessage;

                    string messageId = @"\bMessageId\d+\b";
                    string userId = @"\bUserId\d+\b";
                    Match match = Regex.Match(originalMsg.Text, messageId);
                    Match matchUserId = Regex.Match(originalMsg.Text, userId);

                    await mediator.Send(new RequestMessageCommand { TgUserId = long.Parse(matchUserId.Value.Replace("UserId", "")), ResultMessage = msg.Text, MessageId = int.Parse(match.Value.Replace("MessageId", "")) });
                    await bot.SendTextMessageAsync(chatId: msg.Chat.Id, text: "Запрос от модератора обработан");

                } else
                {
                    await mediator.Send(new RequestCommand { TgUserId = msg.From.Id, Message = msg.Text, MessageId =  msg.MessageId });
                    await bot.SendTextMessageAsync(chatId: "619367092", text: $"Admin Message\r\nMessageId{msg.MessageId}\r\nUserId{msg.From.Id}\r\nТекст:\r\n{msg.Text}");
                    await bot.SendTextMessageAsync(chatId: msg.Chat.Id, text: "Запрос принят");
                }
            }
        }

        async Task<Message> Usage(Message msg)
        {
            const string usage = """
                <b><u>Bot menu</u></b>:
                S-{number} - send status
            """;
            return await bot.SendTextMessageAsync(msg.Chat, usage, parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove());

        }

        async Task<Message> SendPhoto(Message msg)
        {
            await bot.SendChatActionAsync(msg.Chat, ChatAction.UploadPhoto);
            await Task.Delay(2000); // simulate a long task
            await using var fileStream = new FileStream("Files/bot.gif", FileMode.Open, FileAccess.Read);
            return await bot.SendPhotoAsync(msg.Chat, fileStream, caption: "Read https://telegrambots.github.io/book/");
        }

        // Send inline keyboard. You can process responses in OnCallbackQuery handler
        async Task<Message> SendInlineKeyboard(Message msg)
        {
            var inlineMarkup = new InlineKeyboardMarkup()
                .AddNewRow("🆙 Статистика", "🔗 Домены")
                .AddNewRow("💯 Процентная ставка", "🧑‍Техническая поддержка")
                .AddNewRow("📣 Оповещение")
                .AddNewRow("🏭Смена");
            //.AddNewRow()
            //    .AddButton("WithCallbackData", "CallbackData")
            //    .AddButton(InlineKeyboardButton.WithUrl("WithUrl", "https://github.com/TelegramBots/Telegram.Bot"));

            return await bot.SendTextMessageAsync(msg.Chat, "Меню проекта", replyMarkup: inlineMarkup);
        }

        async Task<Message> SendReplyKeyboard(Message msg)
        {
            var replyMarkup = new ReplyKeyboardMarkup(true)
                .AddNewRow("1.1", "1.2", "1.3")
                .AddNewRow().AddButton("2.1").AddButton("2.2");
            return await bot.SendTextMessageAsync(msg.Chat, "Keyboard buttons:", replyMarkup: replyMarkup);
        }

        async Task<Message> RemoveKeyboard(Message msg)
        {
            return await bot.SendTextMessageAsync(msg.Chat, "Removing keyboard", replyMarkup: new ReplyKeyboardRemove());
        }

        async Task<Message> RequestContactAndLocation(Message msg)
        {
            var replyMarkup = new ReplyKeyboardMarkup()
                .AddButton(KeyboardButton.WithRequestLocation("Location"))
                .AddButton(KeyboardButton.WithRequestContact("Contact"));
            return await bot.SendTextMessageAsync(msg.Chat, "Who or Where are you?", replyMarkup: replyMarkup);
        }

        async Task<Message> StartInlineQuery(Message msg)
        {
            var button = InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Inline Mode");
            return await bot.SendTextMessageAsync(msg.Chat, "Press the button to start Inline Query\n\n" +
                "(Make sure you enabled Inline Mode in @BotFather)", replyMarkup: new InlineKeyboardMarkup(button));
        }

        async Task<Message> SendPoll(Message msg)
        {
            return await bot.SendPollAsync(msg.Chat, "Question", PollOptions, isAnonymous: false);
        }

        async Task<Message> SendAnonymousPoll(Message msg)
        {
            return await bot.SendPollAsync(chatId: msg.Chat, "Question", PollOptions);
        }

        static Task<Message> FailingHandler(Message msg)
        {
            throw new NotImplementedException("FailingHandler");
        }

        // Process Inline Keyboard callback data
        private async Task OnCallbackQuery(CallbackQuery callbackQuery)
        {
            logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);
            await bot.AnswerCallbackQueryAsync(callbackQuery.Id, $"Received {callbackQuery.Data}");
            await bot.SendTextMessageAsync(callbackQuery.Message!.Chat, $"Received {callbackQuery.Data}");
        }

        #region Inline Mode

        private async Task OnInlineQuery(InlineQuery inlineQuery)
        {
            logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

            InlineQueryResult[] results = [ // displayed result
                new InlineQueryResultArticle("1", "Telegram.Bot", new InputTextMessageContent("hello")),
            new InlineQueryResultArticle("2", "is the best", new InputTextMessageContent("world"))
            ];
            await bot.AnswerInlineQueryAsync(inlineQuery.Id, results, cacheTime: 0, isPersonal: true);
        }

        private async Task OnChosenInlineResult(ChosenInlineResult chosenInlineResult)
        {
            logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);
            await bot.SendTextMessageAsync(chosenInlineResult.From.Id, $"You chose result with Id: {chosenInlineResult.ResultId}");
        }

        #endregion

        private Task OnPoll(Poll poll)
        {
            logger.LogInformation("Received Poll info: {Question}", poll.Question);
            return Task.CompletedTask;
        }

        private async Task OnPollAnswer(PollAnswer pollAnswer)
        {
            var answer = pollAnswer.OptionIds.FirstOrDefault();
            var selectedOption = PollOptions[answer];
            if (pollAnswer.User != null)
                await bot.SendTextMessageAsync(pollAnswer.User.Id, $"You've chosen: {selectedOption.Text} in poll");
        }

        private Task UnknownUpdateHandlerAsync(Update update)
        {
            logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
            return Task.CompletedTask;
        }
    }
}