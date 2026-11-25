namespace VacApp_Bovinova_Platform.IoTMonitoring.Interfaces.REST.Resources;

/// <summary>
/// DTO de respuesta para la predicción de riesgo sanitario basada en IA (Machine Learning ONNX).
/// </summary>
public record BovineHealthPredictionResource(
    int BovineId,
    string BovineName,
    float RiskPercentage,
    string RiskLevel,
    string Badge,
    int PredictedClass,
    string TimeHorizon,
    string PrimaryFactor,
    string RecommendedAction,
    float CurrentTemperature,
    float CurrentHeartRate,
    float TempSlope3h,
    float HrSlope3h,
    float TempVariability3h,
    float ThiIndex,
    int ReadingsEvaluated,
    DateTime EvaluatedAt
);
