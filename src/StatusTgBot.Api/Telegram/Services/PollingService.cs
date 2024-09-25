using StatusTgBot.Api.Telegram.Abstract;

namespace StatusTgBot.Api.Telegram.Services
{
    public class PollingService(IServiceProvider serviceProvider, ILogger<PollingService> logger)
        : PollingServiceBase<ReceiverService>(serviceProvider, logger);
}
