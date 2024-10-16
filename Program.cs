var builder = WebApplication.CreateBuilder(args);
// Crear instancia de Startup y pasarle la configuración
var startup = new Startup(builder.Configuration);

// Llamar a ConfigureServices del Startup
startup.ConfigureServices(builder.Services);


var app = builder.Build();
startup.Configure(app);

app.Run();
