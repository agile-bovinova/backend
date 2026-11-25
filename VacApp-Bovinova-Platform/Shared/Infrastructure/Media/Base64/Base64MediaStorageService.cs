using VacApp_Bovinova_Platform.Shared.Application.OutboundServices;

namespace VacApp_Bovinova_Platform.Shared.Infrastructure.Media.Base64;

public class Base64MediaStorageService : IMediaStorageService
{
    public string UploadFileAsync(string fileName, Stream fileData)
    {
        if (fileData == null || fileData.Length == 0)
            return string.Empty;

        using var memoryStream = new MemoryStream();
        fileData.CopyTo(memoryStream);
        var bytes = memoryStream.ToArray();

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var mime = ext switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };

        return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
    }

    public void UpdateFileAsync(string url, Stream fileData)
    {
        // En almacenamiento Base64 en BD, la actualización se realiza al persistir la entidad.
    }
}
