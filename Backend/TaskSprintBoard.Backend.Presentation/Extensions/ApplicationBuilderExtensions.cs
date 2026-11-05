using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Seed;
using TaskSprintBoard.Backend.Presentation.Database;

namespace TaskSprintBoard.Backend.Presentation.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication InitializeDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TaskSprintBoardDbContext>();
        LegacySchemaMigrator.ArchiveLegacyTables(dbContext);
        dbContext.Database.Migrate();
        LegacySchemaMigrator.ImportArchivedData(dbContext);

        if (app.Environment.IsDevelopment())
            DatabaseSeed.Seed(dbContext);

        return app;
    }
}
