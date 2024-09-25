using Microsoft.Extensions.Options;
using StatusTgBot.Api.Telegram.Services;
using StatusTgBot.Api;
using Telegram.Bot;
using StatusTgBot.Api.Data;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Hangfire;
using StatusTgBot.Api.BackgroundJobs;
using Newtonsoft.Json;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContextFactory<ApplicationDbContext>(o => o.UseNpgsql(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<BotConfiguration>(builder.Configuration.GetSection("BotConfiguration"));

builder.Services.AddHttpClient("telegram_bot_client").RemoveAllLoggers()
        .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
        {
            BotConfiguration? botConfiguration = sp.GetService<IOptions<BotConfiguration>>()?.Value;
            ArgumentNullException.ThrowIfNull(botConfiguration);
            TelegramBotClientOptions options = new(botConfiguration.BotToken);
            return new TelegramBotClient(options, httpClient);
        });


builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddScoped<UpdateHandler>();
builder.Services.AddScoped<ReceiverService>();
builder.Services.AddHostedService<PollingService>();

// Add Hangfire services.
builder.Services.AddHangfire(c => c
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings(settings => settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore)
            .UsePostgreSqlStorage(connectionString, new PostgreSqlStorageOptions()));

builder.Services.AddHangfireServer();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHangfireDashboard();

RecurringJob.AddOrUpdate<SendResultJob>(nameof(SendResultJob), x => x.Run(), Cron.MinuteInterval(1));
app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
