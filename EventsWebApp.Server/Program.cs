using EventsWebApp.Server;

var builder = WebApplication.CreateBuilder(args);
var startup = new Startup(builder.Configuration);

startup.ConfigureServices(builder.Services);

var app = builder.Build();
startup.Configure(app, app.Environment, args);

app.MapControllers();
app.MapFallbackToFile("/index.html");

app.Run();
