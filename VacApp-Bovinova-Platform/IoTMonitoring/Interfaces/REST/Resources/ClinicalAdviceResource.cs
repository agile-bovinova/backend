namespace VacApp_Bovinova_Platform.IoTMonitoring.Interfaces.REST.Resources;

public record ClinicalAdviceResource(
    int BovineId,
    string BovineName,
    string ClinicalSummary,
    string PrescribedMedicine,
    string DosageAndRoute,
    string InventoryStatus, // "InStock" | "OutOfStock" | "NoInventory"
    string InventoryDetails,
    List<string> FirstAidMeasures,
    string FollowUpInstructions,
    string Source, // "Gemini-AI" | "Embedded-Clinical-Engine"
    DateTime GeneratedAt
);
