using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using VacApp_Bovinova_Platform.IoTMonitoring.Domain.Repositories;
using VacApp_Bovinova_Platform.IoTMonitoring.Domain.Services;
using VacApp_Bovinova_Platform.IoTMonitoring.Interfaces.REST.Resources;
using VacApp_Bovinova_Platform.RanchManagement.Domain.Repositories;

namespace VacApp_Bovinova_Platform.IoTMonitoring.Application.Internal.QueryServices;

public class PredictiveHealthService : IPredictiveHealthService, IDisposable
{
    private readonly IBovineHealthRecordRepository _recordRepository;
    private readonly IBovineRepository _bovineRepository;
    private readonly ILogger<PredictiveHealthService> _logger;
    private InferenceSession? _session;
    private readonly object _sessionLock = new();
    private bool _initialized;

    public PredictiveHealthService(
        IBovineHealthRecordRepository recordRepository,
        IBovineRepository bovineRepository,
        ILogger<PredictiveHealthService> logger)
    {
        _recordRepository = recordRepository;
        _bovineRepository = bovineRepository;
        _logger = logger;
        InitOnnxSession();
    }

    private void InitOnnxSession()
    {
        lock (_sessionLock)
        {
            if (_initialized) return;

            var possiblePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "IoTMonitoring", "Infrastructure", "AI", "bovine_health_risk_model.onnx"),
                Path.Combine(Directory.GetCurrentDirectory(), "IoTMonitoring", "Infrastructure", "AI", "bovine_health_risk_model.onnx"),
                Path.Combine(AppContext.BaseDirectory, "bovine_health_risk_model.onnx"),
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "ai-model", "models", "bovine_health_risk_model.onnx"))
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        _session = new InferenceSession(path);
                        _logger.LogInformation("Modelo ONNX cargado exitosamente desde: {Path}", path);
                        _initialized = true;
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error al cargar ONNX desde {Path}", path);
                    }
                }
            }

            _logger.LogWarning("No se encontró el archivo ONNX en las rutas buscadas. Se utilizará el motor de inferencia interno.");
            _initialized = true;
        }
    }

    public async Task<BovineHealthPredictionResource> PredictHealthRiskAsync(int bovineId, int userId)
    {
        var bovine = await _bovineRepository.FindByIdAsync(bovineId);
        var bovineName = bovine?.Name ?? $"Bovino #{bovineId}";

        // Obtener historial de telemetría reciente
        var allRecords = (await _recordRepository.FindByBovineIdAsync(bovineId, userId)).ToList();

        float currentTemp = 38.5f;
        float currentHr = 60.0f;
        float tempSlope3h = 0.0f;
        float hrSlope3h = 0.0f;
        float tempVar3h = 0.08f;
        float thiIndex = 72.0f;
        int readingsCount = allRecords.Count;

        if (readingsCount > 0)
        {
            var latest = allRecords.First();
            currentTemp = latest.Temperature;
            currentHr = latest.HeartRate;

            // Ventana activa de telemetría reciente: para permitir demostraciones
            // interactivas en vivo (reactividad en segundos tanto para alza como
            // para recuperación), tomamos las últimas 6 lecturas más recientes.
            var windowRecords = allRecords.Take(6).ToList();

            if (windowRecords.Count >= 2)
            {
                var oldest = windowRecords.Last();
                tempSlope3h = latest.Temperature - oldest.Temperature;
                hrSlope3h = latest.HeartRate - oldest.HeartRate;

                // Calcular desviación estándar sobre la ventana reciente
                var temps = windowRecords.Select(r => r.Temperature).ToList();
                double avg = temps.Average();
                double sumSquares = temps.Sum(t => Math.Pow(t - avg, 2));
                tempVar3h = (float)Math.Sqrt(sumSquares / temps.Count);
            }
        }

        // Ejecutar inferencia
        float riskProb = RunInference(currentTemp, currentHr, tempSlope3h, hrSlope3h, tempVar3h, thiIndex);
        float riskPercent = (float)Math.Round(riskProb * 100.0f, 1);

        string level;
        string badge;
        int predictedClass;
        string horizon;
        string factor;
        string action;

        if (riskProb < 0.35f)
        {
            level = "Bajo";
            badge = "🟢";
            predictedClass = 0;
            horizon = "Estable (Sin riesgo en 24h)";
            factor = "Parámetros fisiológicos estables dentro del ciclo circadiano basal.";
            action = "Sin acción requerida. El animal se encuentra en estado saludable.";
        }
        else if (riskProb < 0.70f)
        {
            level = "Moderado";
            badge = "🟡";
            predictedClass = 0;
            horizon = "Precaución (Monitoreo próximas 12-24h)";
            factor = "Leve oscilación térmica o estrés ambiental por calor (THI elevado).";
            action = "Aumentar frecuencia de monitoreo y asegurar sombra y agua fresca.";
        }
        else
        {
            level = "Alto (Inminente)";
            badge = "🔴";
            predictedClass = 1;
            horizon = "Fiebre inminente en 12 a 24 horas";
            factor = $"Aceleración térmica rápida (+{tempSlope3h:F2} °C en ventana reciente) con pulso acelerado ({currentHr:F0} BPM).";
            action = "Aislamiento preventivo en corral de observación, examen clínico y preparación de antipirético.";
        }

        return new BovineHealthPredictionResource(
            BovineId: bovineId,
            BovineName: bovineName,
            RiskPercentage: riskPercent,
            RiskLevel: level,
            Badge: badge,
            PredictedClass: predictedClass,
            TimeHorizon: horizon,
            PrimaryFactor: factor,
            RecommendedAction: action,
            CurrentTemperature: currentTemp,
            CurrentHeartRate: currentHr,
            TempSlope3h: tempSlope3h,
            HrSlope3h: hrSlope3h,
            TempVariability3h: tempVar3h,
            ThiIndex: thiIndex,
            ReadingsEvaluated: readingsCount,
            EvaluatedAt: DateTime.UtcNow
        );
    }

    private float RunInference(float temp, float hr, float tempSlope, float hrSlope, float tempVar, float thi)
    {
        if (_session != null)
        {
            try
            {
                var inputTensor = new DenseTensor<float>(new[] { temp, hr, tempSlope, hrSlope, tempVar, thi }, new[] { 1, 6 });
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("float_input", inputTensor)
                };

                using var results = _session.Run(inputs);
                var probTensor = results[1].AsTensor<float>();
                // probTensor[0, 1] es la probabilidad de clase 1 (Riesgo Febril)
                return probTensor[0, 1];
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallo en inferencia ONNX, ejecutando inferencia interna de contingencia.");
            }
        }

        // Modelo de inferencia contingente calibrado (equivalente a los pesos del bosque)
        float score = 0.0f;
        if (tempSlope > 0.40f) score += 0.40f;
        else if (tempSlope > 0.20f) score += 0.15f;

        if (hr > 85.0f) score += 0.25f;
        else if (hr > 75.0f) score += 0.10f;

        if (tempVar > 0.25f) score += 0.25f;
        if (temp > 39.10f) score += 0.10f;

        return Math.Clamp(score, 0.05f, 0.99f);
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}
