using VacApp_Bovinova_Platform.IoTMonitoring.Interfaces.REST.Resources;

namespace VacApp_Bovinova_Platform.IoTMonitoring.Domain.Services;

public interface IClinicalAdvisorService
{
    Task<ClinicalAdviceResource> GenerateClinicalAdviceAsync(int bovineId, int userId);
}
