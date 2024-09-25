using MediatR;

namespace StatusTgBot.Api.Infrastructure.Commands
{
    public class RequestMessageCommand : IRequest<RequestMessageResultCommand>
    {
        public long TgUserId { get; set; }
        public int MessageId { get; set; }
        public string ResultMessage { get; set; }
    }

    public class RequestMessageResultCommand : CommandResult
    {
    }
}
