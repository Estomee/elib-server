// Interface defining object storage operations: get, upload, presigned URL generation, and delete.
namespace Server.Services;

public interface IStorageService
{
    Task<string> GetFile(string storageKey);

    Task<string> GetUploadUrl(string storageKey, string contentType);

    Task<string> UploadFile(string storageKey, Stream content, string contentType);

    Task DeleteFile(string storageKey);
}
