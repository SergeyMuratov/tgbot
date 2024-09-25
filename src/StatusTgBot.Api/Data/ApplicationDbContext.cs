using Microsoft.EntityFrameworkCore;
using StatusTgBot.Api.Data.Models;

namespace StatusTgBot.Api.Data
{
    public class ApplicationDbContext:  DbContext
    {
        public ApplicationDbContext()
        {
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Request> Requests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Request>(b =>
            {
                b.HasKey(x => x.Id);
                b.ToTable("Request");
            });
        }
    }
}
