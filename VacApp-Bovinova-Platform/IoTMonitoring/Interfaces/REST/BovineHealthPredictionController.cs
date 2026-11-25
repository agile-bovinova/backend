using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using VacApp_Bovinova_Platform.IAM.Domain.Model.Aggregates;
using VacApp_Bovinova_Platform.IAM.Infrastructure.Pipeline.Middleware.Attributes;
using VacApp_Bovinova_Platform.IoTMonitoring.Domain.Services;
using VacApp_Bovinova_Platform.IoTMonitoring.Interfaces.REST.Resources;

namespace VacApp_Bovinova_Platform.IoTMonitoring.Interfaces.REST;

[Authorize]
[ApiController]
[Route("api/v1/bovines/{bovineId:int}/health-prediction")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Predictive AI - Animal Health")]
public class BovineHealthPredictionController(
    IPredictiveHealthService predictiveHealthService,
    IClinicalAdvisorService clinicalAdvisorService)
    : ControllerBase
{
    /// <summary>
    /// Evalúa el riesgo predictivo de fiebre en las próximas 12 a 24 horas usando Inteligencia Artificial (ONNX).
    /// Analiza la ventana reciente de telemetría (temperatura, aceleración térmica, pulso y variabilidad).
    /// </summary>
    /// <param name="bovineId">Identificador del bovino.</param>
    /// <returns>Evaluación predictiva con porcentaje de riesgo y recomendación clínica.</returns>
    [AllowAnonymous]
    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK, "Predicción obtenida exitosamente", typeof(BovineHealthPredictionResource))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "No autorizado")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Bovino no encontrado")]
    public async Task<IActionResult> GetHealthPrediction(int bovineId)
    {
        var user = HttpContext.Items["User"] as User;
        int userId = user?.Id ?? 1;

        var result = await predictiveHealthService.PredictHealthRiskAsync(bovineId, userId);
        return Ok(result);
    }

    /// <summary>
    /// Genera una prescripción veterinaria personalizada mediante IA cruzando la telemetría del animal con el inventario real del rancho.
    /// Soporte dual: consulta Gemini API en la nube con respaldo local inteligente ante desconexión de red.
    /// </summary>
    /// <param name="bovineId">Identificador del bovino.</param>
    /// <returns>Ficha clínica con medicamento, dosis por peso, disponibilidad en almacén y primeros auxilios físicos.</returns>
    [AllowAnonymous]
    [HttpGet("clinical-advice")]
    [SwaggerResponse(StatusCodes.Status200OK, "Prescripción clínica generada exitosamente", typeof(ClinicalAdviceResource))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "No autorizado")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Bovino no encontrado")]
    public async Task<IActionResult> GetClinicalAdvice(int bovineId)
    {
        var user = HttpContext.Items["User"] as User;
        int userId = user?.Id ?? 1;

        var result = await clinicalAdvisorService.GenerateClinicalAdviceAsync(bovineId, userId);
        return Ok(result);
    }
}
