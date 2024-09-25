namespace StatusTgBot.Api.Data.Models
{
    public class Request : Entity
    {
        public Request()
        {
            CreateAt = DateTime.UtcNow;
        }


        public long TgUserId { get; set; }
        public int MessageId { get; set; }
        public string Message { get; set; }
        public string? ResultMessage { get; set; }
        public bool IsSent { get; set; }

        public DateTime CreateAt { get; set; }
    }
}
