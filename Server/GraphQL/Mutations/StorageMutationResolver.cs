// GraphQL mutation resolver that generates presigned S3 upload URLs for book covers and content files.
using HotChocolate;
using HotChocolate.Authorization;
using Server.GraphQL.Types;
using Server.Services;

namespace Server.GraphQL.Mutations;

[ExtendObjectType<Mutation>]
public class StorageMutationResolver
{
    private static readonly Dictionary<string, string> _extMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"]              = ".jpg",
        ["image/png"]               = ".png",
        ["image/webp"]              = ".webp",
        ["application/pdf"]         = ".pdf",
        ["application/epub+zip"]    = ".epub",
    };

    private static readonly HashSet<string> _imageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    [GraphQLName("get_upload_url")]
    [Authorize]
    public async Task<UploadUrlResult> GetUploadUrl(
        string contentType,
        [Service] IStorageService storage)
    {
        var ext    = _extMap.TryGetValue(contentType, out var e) ? e : ".bin";
        var folder = _imageTypes.Contains(contentType) ? "covers" : "book_files";
        var storageKey = $"{folder}/{Guid.NewGuid()}{ext}";
        var url        = await storage.GetUploadUrl(storageKey, contentType);
        return new UploadUrlResult { PresignedUrl = url, StorageKey = storageKey };
    }
}
