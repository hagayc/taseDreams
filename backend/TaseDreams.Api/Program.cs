using Microsoft.EntityFrameworkCore;
using TaseDreams.Api.Configuration;
using TaseDreams.Api.Data;
using TaseDreams.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Services
builder.Services.AddScoped<IBitbucketService, BitbucketService>();
builder.Services.AddScoped<IArtifactoryService, ArtifactoryService>();
builder.Services.AddScoped<IArgoCDService, ArgoCDService>();
builder.Services.AddScoped<IOCPService, OCPService>();
builder.Services.AddScoped<IConfluenceService, ConfluenceService>();
builder.Services.AddScoped<ILdapService, LdapService>();
builder.Services.AddScoped<IProjectCreationService, ProjectCreationService>();

// Configuration
builder.Services.Configure<BitbucketConfig>(builder.Configuration.GetSection("Bitbucket"));
builder.Services.Configure<ArtifactoryConfig>(builder.Configuration.GetSection("Artifactory"));
builder.Services.Configure<ArgoCDConfig>(builder.Configuration.GetSection("ArgoCD"));
builder.Services.Configure<OCPConfig>(builder.Configuration.GetSection("OCP"));
builder.Services.Configure<ConfluenceConfig>(builder.Configuration.GetSection("Confluence"));
builder.Services.Configure<LdapConfig>(builder.Configuration.GetSection("LDAP"));
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");
app.UseAuthorization();
app.MapControllers();

// Ensure database is created (with retry logic in background)
_ = Task.Run(async () =>
{
    await Task.Delay(TimeSpan.FromSeconds(5)); // Give database time to be ready
    
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var maxRetries = 10;
        var delay = TimeSpan.FromSeconds(3);
        
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                db.Database.EnsureCreated();
                logger.LogInformation("Database initialized successfully");
                break;
            }
            catch (Exception ex)
            {
                if (i == maxRetries - 1)
                {
                    logger.LogError(ex, "Failed to create database after {Retries} attempts", maxRetries);
                }
                else
                {
                    logger.LogWarning("Database not ready, retrying in {Delay} seconds... (Attempt {Attempt}/{MaxRetries})", 
                        delay.TotalSeconds, i + 1, maxRetries);
                    await Task.Delay(delay);
                }
            }
        }
    }
});

app.Run();

