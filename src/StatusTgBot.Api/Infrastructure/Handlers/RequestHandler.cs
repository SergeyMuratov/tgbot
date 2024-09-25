using MediatR;
using StatusTgBot.Api.Data;
using StatusTgBot.Api.Data.Models;
using StatusTgBot.Api.Infrastructure.Commands;

namespace StatusTgBot.Api.Infrastructure.Handlers
{
    public class RequestHandler : IRequestHandler<RequestCommand, RequestResult>
    {
        private readonly ApplicationDbContext _context;

        public RequestHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RequestResult> Handle(RequestCommand request, CancellationToken cancellationToken)
        {
            await _context.AddAsync(new Request()
            {
                TgUserId = request.TgUserId,
                MessageId = request.MessageId,
                Message = request.Message,
            });

            await _context.SaveChangesAsync();

            return new RequestResult { Successfully = true };
        }
    }
}
