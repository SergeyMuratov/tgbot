using StatusTgBot.Api.Telegram.Abstract;
using Telegram.Bot;

namespace StatusTgBot.Api.Telegram.Services
{
    public class ReceiverService(ITelegramBotClient botClient, UpdateHandler updateHandler, ILogger<ReceiverServiceBase<UpdateHandler>> logger)
        : ReceiverServiceBase<UpdateHandler>(botClient, updateHandler, logger);
}
