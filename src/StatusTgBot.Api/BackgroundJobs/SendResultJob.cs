using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StatusTgBot.Api.Data;
using StatusTgBot.Api.Data.Models;
using StatusTgBot.Api.Infrastructure.Commands;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace StatusTgBot.Api.BackgroundJobs
{
    [AutomaticRetry(Attempts = 0)]
    public class SendResultJob
    {
        private readonly ApplicationDbContext _context;
        private readonly IMediator _mediator;
        private readonly ITelegramBotClient _bot;
        public SendResultJob(ApplicationDbContext context, IMediator mediator, ITelegramBotClient bot)
        {
            _context = context;
            _mediator = mediator;
            _bot = bot;
        }


        [DisableConcurrentExecution(timeoutInSeconds: 360)]
        public async Task Run()
        {
           var requests = await _context.Set<Request>()
            .Where(x => x.IsSent == false)
            .Where(x => x.ResultMessage != null)
            .ToListAsync();

            foreach (var request in requests)
            {
                try
                {
                    await _bot.SendTextMessageAsync(chatId: request.TgUserId, text: request.ResultMessage);

                    request.IsSent = true;
                    await _context.SaveChangesAsync();

                }
                catch (Exception ex)
                {
                }
            }
        }
    }
}
