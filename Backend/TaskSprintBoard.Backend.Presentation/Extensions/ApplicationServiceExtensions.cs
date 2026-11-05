using TaskSprintBoard.Backend.BusinessLogic.Services;
using TaskSprintBoard.Backend.Data.Repositories;

namespace TaskSprintBoard.Backend.Presentation.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ProjectRepository>();
        services.AddScoped<SprintRepository>();
        services.AddScoped<TaskRepository>();
        services.AddScoped<UserRepository>();
        services.AddScoped<AssignmentRepository>();
        services.AddScoped<CommentRepository>();
        services.AddScoped<StatusHistoryRepository>();
        services.AddScoped<TaskRiskEvaluationRepository>();
        services.AddScoped<AuditLogRepository>();

        services.AddScoped<ProjectService>();
        services.AddScoped<SprintService>();
        services.AddScoped<TaskService>();
        services.AddScoped<AssignmentService>();
        services.AddScoped<CommentService>();
        services.AddScoped<StatusHistoryService>();
        services.AddScoped<AuthService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<AiRiskService>();
        services.AddScoped<UserService>();
        services.AddScoped<AuditLogService>();

        return services;
    }
}
