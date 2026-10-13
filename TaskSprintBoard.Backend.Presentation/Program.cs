using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TaskSprintBoard.Backend.BusinessLogic.Services;
using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Repositories;
using TaskSprintBoard.Backend.Data.Seed;
using TaskSprintBoard.Backend.Presentation.Database;
using TaskSprintBoard.Backend.Presentation.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<TaskSprintBoardDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ??
        "Server=(localdb)\\mssqllocaldb;Database=TaskSprintBoardDb;Trusted_Connection=True;MultipleActiveResultSets=true"));

builder.Services.AddScoped<ProjectRepository>();
builder.Services.AddScoped<SprintRepository>();
builder.Services.AddScoped<TaskRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<AssignmentRepository>();
builder.Services.AddScoped<CommentRepository>();
builder.Services.AddScoped<StatusHistoryRepository>();
builder.Services.AddScoped<TaskRiskEvaluationRepository>();

builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<SprintService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<AssignmentService>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddScoped<StatusHistoryService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<AiRiskService>();
builder.Services.AddScoped<UserService>();

var configuredJwtKey = builder.Configuration["Jwt:Key"];
var jwtKey = !string.IsNullOrWhiteSpace(configuredJwtKey)
    ? configuredJwtKey
    : Environment.GetEnvironmentVariable("TASKSPRINTBOARD_JWT_KEY");
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("JWT signing key is missing. Set Jwt:Key or the TASKSPRINTBOARD_JWT_KEY environment variable.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TaskSprintBoard";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TaskSprintBoardClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireProjectManager", policy => policy.RequireRole("ProjectManager"));
    options.AddPolicy("RequireTeamMember", policy => policy.RequireRole("TeamMember"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskSprintBoard API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

// Keep the configured database schema in sync before the API starts accepting requests.
// Without this, a fresh local database has no tables and every write fails at SaveChanges.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TaskSprintBoardDbContext>();
    LegacySchemaMigrator.ArchiveLegacyTables(dbContext);
    dbContext.Database.Migrate();
    LegacySchemaMigrator.ImportArchivedData(dbContext);

    if (app.Environment.IsDevelopment())
    {
        DatabaseSeed.Seed(dbContext);
    }
}

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
