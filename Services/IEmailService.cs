using System.Threading.Tasks;
using System.Collections.Generic;

namespace ProjectEvaluationSystem.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string htmlMessage);
        Task SendProjectEvaluationSummaryAsync(int projectId);
        Task SendAnonymousEvaluationNotificationAsync(int evaluationId);
    }
}