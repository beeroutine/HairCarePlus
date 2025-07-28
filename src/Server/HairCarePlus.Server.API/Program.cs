using HairCarePlus.Shared.Communication.Events;
using HairCarePlus.Server.Infrastructure.RealTime;
using HairCarePlus.Server.Application.PhotoReports;
using MediatR;
using Microsoft.EntityFrameworkCore;
using HairCarePlus.Server.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure logging to console and set minimum level for our namespace
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFilter("HairCarePlus", LogLevel.Information);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddMediatR(typeof(HairCarePlus.Server.Infrastructure.RealTime.EventsHub).Assembly);
builder.Services.AddMediatR(typeof(HairCarePlus.Server.Application.PhotoReports.CreatePhotoReportCommand).Assembly);
builder.Services.Configure<HairCarePlus.Server.Infrastructure.DeliveryOptions>(builder.Configuration.GetSection("Delivery"));

builder.Services.AddSingleton<IEventsClient, EventsClientProxy>();

builder.Services.AddDbContext<HairCarePlus.Server.Infrastructure.Data.AppDbContext>(options =>
{
    options.UseSqlite("Data Source=haircareplus.db");
});

builder.Services.AddScoped<HairCarePlus.Server.Infrastructure.Data.Repositories.IDeliveryQueueRepository,
                       HairCarePlus.Server.Infrastructure.Data.Repositories.DeliveryQueueRepository>();
builder.Services.AddHostedService<HairCarePlus.Server.Infrastructure.Data.DeliveryQueueCleaner>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
app.UseCors();

var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
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

app.MapHub<HairCarePlus.Server.API.Controllers.ChatHub>("/chatHub");
app.MapHub<EventsHub>("/events");

app.MapControllers();

app.Urls.Add("http://0.0.0.0:5281");

// Apply pending EF Core migrations to ensure schema up-to-date
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
