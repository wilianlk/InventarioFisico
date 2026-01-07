using InventarioFisico.Infrastructure;
using InventarioFisico.Repositories;
using InventarioFisico.Services;
using InventarioFisico.Services.Auth;
using Microsoft.Extensions.FileProviders;
using Serilog;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ContentRootPath = AppContext.BaseDirectory,
    WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot")
});

// ============================================
// CREACIÓN DE CARPETAS NECESARIAS
// ============================================
Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Logs"));
Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "DI81"));

// ============================================
// CONFIGURACIÓN DE SERILOG (CONSOLA + ARCHIVO)
// ============================================
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// ============================================
// CONFIGURACIÓN DE SERVICIOS
// ============================================
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ============================================
// CONFIGURACIÓN DE CORS
// ============================================
builder.Services.AddCors(options =>
{
    // Entorno interno (React o pruebas locales)
    options.AddPolicy("AllowInternal", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });

    // Política abierta solo para desarrollo rápido
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ============================================
// DEPENDENCIAS DEL PROYECTO
// ============================================
builder.Services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();

// Repositorios
builder.Services.AddScoped<InventarioRepository>();
builder.Services.AddScoped<ConsolidacionRepository>();
builder.Services.AddScoped<AuditoriaRepository>();
builder.Services.AddScoped<GrupoConteoRepository>();
builder.Services.AddScoped<GrupoUbicacionRepository>();
builder.Services.AddScoped<GrupoPersonaRepository>();
builder.Services.AddScoped<BloqueConteoRepository>();
builder.Services.AddScoped<OperacionConteoRepository>();
builder.Services.AddScoped<OperacionConteoItemsRepository>();

// Servicios
builder.Services.AddScoped<InventarioService>();
builder.Services.AddScoped<ConsolidacionService>();
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddScoped<GrupoConteoService>();
builder.Services.AddScoped<GrupoUbicacionService>();
builder.Services.AddScoped<GrupoPersonaService>();
builder.Services.AddScoped<BloqueConteoService>();
builder.Services.AddScoped<GeneradorConteoService>();
builder.Services.AddScoped<ValidacionCierreService>();
builder.Services.AddScoped<OperacionConteoItemsService>();
builder.Services.AddScoped<CerrarConteoService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// ============================================
// PIPELINE HTTP
// ============================================
app.UseHttpsRedirection();

// Servir archivos generados (DI81, logs, etc.)
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "DI81")),
    RequestPath = "/DI81"
});

// Swagger disponible siempre (puedes limitarlo si deseas)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Inventario Físico v1");
    c.RoutePrefix = "swagger";
});

// CORS: interno si está en prod o abierto si estás desarrollando
var corsPolicy = app.Environment.IsDevelopment() ? "AllowAll" : "AllowInternal";
app.UseCors(corsPolicy);

// Logging de peticiones
app.UseSerilogRequestLogging();

app.UseAuthorization();

app.MapControllers();

// ============================================
// OPCIONAL: FALLBACK SPA (SI TU FRONTEND ESTÁ AQUÍ)
// ============================================
if (app.Environment.IsProduction())
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapFallbackToFile("index.html");
}

app.Run();
