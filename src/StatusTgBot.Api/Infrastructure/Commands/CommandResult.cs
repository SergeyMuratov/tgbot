namespace StatusTgBot.Api.Infrastructure.Commands
{
    public abstract class CommandResult
    {
        public bool Successfully { get; set; }
        public string Message { get; set; }
    }
}
