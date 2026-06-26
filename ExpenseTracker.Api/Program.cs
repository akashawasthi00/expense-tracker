using ExpenseTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Services (Dependency Injection container) ---
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register EF Core DbContext using the connection string from configuration.
// Connection string can be overridden by env var (K8s Secret) in production.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Health checks: includes a DB check so Kubernetes readiness probe reflects real state.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(name: "database");

var app = builder.Build();

// --- HTTP request pipeline ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Swagger UI available in all environments for easy testing after deploy.
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

// Liveness: is the process up?  Readiness: are dependencies (DB) ready?
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

// Apply any pending EF Core migrations automatically on startup.
// Convenient for demos/containers; for prod you may prefer a separate migration job.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();
