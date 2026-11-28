using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VacApp_Bovinova_Platform.IoTMonitoring.Domain.Services;
using VacApp_Bovinova_Platform.IoTMonitoring.Interfaces.REST.Resources;
using VacApp_Bovinova_Platform.RanchManagement.Domain.Model.Aggregates;
using VacApp_Bovinova_Platform.RanchManagement.Domain.Repositories;

namespace VacApp_Bovinova_Platform.IoTMonitoring.Application.Internal.QueryServices;

public class ClinicalAdvisorService(
    IBovineRepository bovineRepository,
    IProductRepository productRepository,
    IPredictiveHealthService predictiveHealthService,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<ClinicalAdvisorService> logger) : IClinicalAdvisorService
{
    public async Task<ClinicalAdviceResource> GenerateClinicalAdviceAsync(int bovineId, int userId)
    {
        var bovine = await bovineRepository.FindByIdAsync(bovineId);
        var bovineName = bovine?.Name ?? $"Bovino #{bovineId}";
        var prediction = await predictiveHealthService.PredictHealthRiskAsync(bovineId, userId);
        var products = (await productRepository.FindByUserIdAsync(userId)).ToList();

        // 1. Intentar consulta a Gemini API si existe la API Key configurada
        var apiKey = configuration["GEMINI_API_KEY"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var geminiResult = await CallGeminiAdvisorAsync(apiKey, bovine, prediction, products);
                if (geminiResult != null)
                {
                    logger.LogInformation("Prescripción clínica generada exitosamente con Gemini AI para bovino {BovineId}", bovineId);
                    return geminiResult;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Fallo al consultar Gemini API (timeout o error de red). Activando motor clínico local...");
            }
        }

        // 2. Soporte inteligente local (Fallback en 0 ms sin depender de internet)
        return GenerateLocalClinicalAdvice(bovineId, bovineName, prediction, products);
    }

    private async Task<ClinicalAdviceResource?> CallGeminiAdvisorAsync(
        string apiKey,
        Bovine? bovine,
        BovineHealthPredictionResource prediction,
        List<Product> products)
    {
        using var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(8); // Timeout seguro para Gemini 2.5 con respaldo ante bloqueo de campus

        var availableProductsText = products.Count > 0
            ? string.Join(", ", products.Select(p => $"{p.Name} (Stock: {p.Quantity} {p.Unit ?? "unidades"})"))
            : "Inventario vacío / Sin productos registrados.";

        var prompt = $@"
Actúa como un médico veterinario especialista en producción bovina.
Analiza la siguiente condición fisiológica de un bovino y cruza los datos con el inventario del rancho:

DATOS DEL BOVINO:
- Nombre: {bovine?.Name ?? "Bovino"}
- Raza: {bovine?.Breed ?? "Bovino lechero / cárnico"}
- Género: {bovine?.Gender ?? "Hembra"}
- Temperatura actual: {prediction.CurrentTemperature:F1} °C
- Frecuencia cardíaca: {prediction.CurrentHeartRate:F0} BPM
- Diagnóstico predictivo de IA: {prediction.RiskLevel} ({prediction.RiskPercentage:F1}% de riesgo febril)
- Factor principal: {prediction.PrimaryFactor}

INVENTARIO DISPONIBLE EN EL RANCHO:
{availableProductsText}

INSTRUCCIONES CLÍNICAS:
1. Si el animal tiene fiebre o riesgo alto, evalúa si en el inventario hay un fármaco compatible (ej. Flunixin Meglumine, Dipirona, antipiréticos).
2. Si hay stock disponible, indícalo con la dosis exacta por peso (asume 550 kg para vacas adultas, ej. 15 ml IM) y marca inventoryStatus como 'InStock'.
3. Si el inventario está vacío o no hay antipiréticos, indica el fármaco genérico comercial que debe comprarse con urgencia, marca inventoryStatus como 'OutOfStock' y provee medidas físicas inmediatas de campo.
4. Genera una lista de primeros auxilios físicos (enfriamiento, sombra, hidratación).
5. Responde ÚNICAMENTE en formato JSON con la siguiente estructura exacta:
{{
  ""clinicalSummary"": ""resumen en una oración"",
  ""prescribedMedicine"": ""nombre del fármaco o indicación de compra"",
  ""dosageAndRoute"": ""dosis en ml y vía de administración"",
  ""inventoryStatus"": ""InStock"" o ""OutOfStock"",
  ""inventoryDetails"": ""descripción de disponibilidad o advertencia"",
  ""firstAidMeasures"": [""medida 1"", ""medida 2"", ""medida 3""],
  ""followUpInstructions"": ""instrucción de seguimiento temporal""
}}";

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                responseMimeType = "application/json",
                thinkingConfig = new { thinkingBudget = 0 }
            }
        };

        var model = configuration["GEMINI_MODEL"] ?? Environment.GetEnvironmentVariable("GEMINI_MODEL");
        if (string.IsNullOrWhiteSpace(model)) model = "gemini-2.5-flash";

        var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var response = await client.PostAsync(url, jsonContent);
        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync();
            logger.LogWarning("Gemini API ({Model}) respondió HTTP {StatusCode}: {Body}", model, (int)response.StatusCode, errBody);
            return null;
        }

        var responseString = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseString);
        var root = doc.RootElement;

        var textNode = root
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(textNode)) return null;

        using var jsonDoc = JsonDocument.Parse(textNode);
        var res = jsonDoc.RootElement;

        var summary = res.GetProperty("clinicalSummary").GetString() ?? "Evaluación veterinaria generada.";
        var med = res.GetProperty("prescribedMedicine").GetString() ?? "Soporte sintomático";
        var dosage = res.GetProperty("dosageAndRoute").GetString() ?? "Según indicación veterinaria";
        var invStatus = res.GetProperty("inventoryStatus").GetString() ?? (products.Count > 0 ? "InStock" : "OutOfStock");
        var invDetails = res.GetProperty("inventoryDetails").GetString() ?? "";

        var firstAid = new List<string>();
        if (res.TryGetProperty("firstAidMeasures", out var faArray))
        {
            foreach (var elem in faArray.EnumerateArray())
            {
                var s = elem.GetString();
                if (!string.IsNullOrWhiteSpace(s)) firstAid.Add(s);
            }
        }

        var followUp = res.GetProperty("followUpInstructions").GetString() ?? "Reevaluar temperatura en 2 a 4 horas.";

        return new ClinicalAdviceResource(
            bovine?.Id ?? 0,
            bovine?.Name ?? "Bovino",
            summary,
            med,
            dosage,
            invStatus,
            invDetails,
            firstAid.Count > 0 ? firstAid : new List<string> { "Traslado a sombra", "Agua fresca", "Monitoreo continuo" },
            followUp,
            "Gemini-AI",
            DateTime.UtcNow
        );
    }

    private ClinicalAdviceResource GenerateLocalClinicalAdvice(
        int bovineId,
        string bovineName,
        BovineHealthPredictionResource prediction,
        List<Product> products)
    {
        bool hasFever = prediction.CurrentTemperature >= 39.5f || prediction.RiskPercentage >= 70.0f;

        // Buscar si en el inventario del usuario existe algún antipirético / antiinflamatorio
        var matchingMedicine = products.FirstOrDefault(p =>
            p.Quantity > 0 && (
                p.Name.Contains("Flunixin", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("Dipirona", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("Antipirético", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("Ketoprofeno", StringComparison.OrdinalIgnoreCase)
            ));

        string summary;
        string medicine;
        string dosage;
        string invStatus;
        string invDetails;
        List<string> firstAid;
        string followUp;

        if (hasFever)
        {
            summary = $"Hipertermia activa ({prediction.CurrentTemperature:F1} °C) con riesgo predictivo de {prediction.RiskPercentage:F1}%. Protocolo de intervención inmediata requerido.";

            if (matchingMedicine != null)
            {
                medicine = matchingMedicine.Name;
                dosage = "15 a 20 ml (2.2 mg/kg) vía intramuscular profunda cada 24 horas";
                invStatus = "InStock";
                invDetails = $"{matchingMedicine.Quantity} {matchingMedicine.Unit ?? "frasco(s)"} en inventario (Disponible en Almacén)";
            }
            else
            {
                medicine = "Flunixin Meglumine 50 mg/ml o Dipirona 50% (Sugerencia de compra urgente)";
                dosage = "15 a 20 ml vía intramuscular profunda";
                invStatus = products.Count > 0 ? "OutOfStock" : "NoInventory";
                invDetails = "No se encontraron antipiréticos con stock disponible en tu inventario.";
            }

            firstAid = new List<string>
            {
                "Aislar inmediatamente al animal en un corral techado con buena ventilación natural o forzada.",
                "Aplicar baño de aspersión con agua a temperatura ambiente en lomo y nuca durante 10 minutos para disipación térmica por evaporación.",
                "Ofrecer agua fresca limpia ad libitum adicionada con sales rehidratantes orales.",
                "Retirar temporalmente raciones de concentrado pesado para prevenir sobrecarga ruminal mientras dure la fiebre."
            };
            followUp = "Reevaluar temperatura corporal y frecuencia cardíaca cada 30 a 45 minutos tras la intervención.";
        }
        else
        {
            summary = $"Parámetros biológicos en rango óptimo ({prediction.CurrentTemperature:F1} °C, {prediction.CurrentHeartRate:F0} BPM). Estado de salud estable.";
            medicine = "Ninguna medicación requerida";
            dosage = "No aplica";
            invStatus = "InStock";
            invDetails = "Parámetros basales normales; no se requiere uso de insumos de almacén.";
            firstAid = new List<string>
            {
                "Mantener el ciclo normal de pastoreo y descanso en establo.",
                "Asegurar acceso constante a bebederos limpios y forraje fresco.",
                "Continuar con la telemetría continua del collar IoT para monitoreo preventivo."
            };
            followUp = "Continuar con el monitoreo rutinario del rebaño.";
        }

        return new ClinicalAdviceResource(
            bovineId,
            bovineName,
            summary,
            medicine,
            dosage,
            invStatus,
            invDetails,
            firstAid,
            followUp,
            "Embedded-Clinical-Engine",
            DateTime.UtcNow
        );
    }
}
