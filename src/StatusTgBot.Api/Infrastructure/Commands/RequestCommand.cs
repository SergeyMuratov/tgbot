using MediatR;

namespace StatusTgBot.Api.Infrastructure.Commands
{
    public class RequestCommand : IRequest<RequestResult>
    {
        public long TgUserId { get; set; }
        public int MessageId { get; set; }
        public string Message { get; set; }
    }

    public class RequestResult : CommandResult
    {
    }
}
