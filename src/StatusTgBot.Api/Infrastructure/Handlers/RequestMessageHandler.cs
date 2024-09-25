
using MediatR;
using Microsoft.EntityFrameworkCore;
using StatusTgBot.Api.Data;
using StatusTgBot.Api.Data.Models;
using StatusTgBot.Api.Infrastructure.Commands;

namespace StatusTgBot.Api.Infrastructure.Handlers
{
    public class RequestMessageHandler : IRequestHandler<RequestMessageCommand, RequestMessageResultCommand>
    {
        private readonly ApplicationDbContext _context;

        public RequestMessageHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RequestMessageResultCommand> Handle(RequestMessageCommand request, CancellationToken cancellationToken)
        {
            var msg = await _context.Set<Request>().SingleOrDefaultAsync(x => x.TgUserId == request.TgUserId && x.MessageId == request.MessageId);

            msg.ResultMessage = request.ResultMessage;
            await _context.SaveChangesAsync();

            return new RequestMessageResultCommand { Successfully = true };
        }
    }
}
