using VacApp_Bovinova_Platform.IoTMonitoring.Interfaces.REST.Resources;

namespace VacApp_Bovinova_Platform.IoTMonitoring.Domain.Services;

public interface IPredictiveHealthService
{
    Task<BovineHealthPredictionResource> PredictHealthRiskAsync(int bovineId, int userId);
}
