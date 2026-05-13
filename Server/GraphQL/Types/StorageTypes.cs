// GraphQL DTO type representing the result of a presigned upload URL request.
using HotChocolate;

namespace Server.GraphQL.Types;

[GraphQLName("UploadUrlResult")]
public class UploadUrlResult
{
    public string PresignedUrl { get; set; } = string.Empty;
    public string StorageKey   { get; set; } = string.Empty;
}
