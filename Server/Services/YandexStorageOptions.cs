// Configuration options for Yandex S3 storage including credentials, region, and bucket names.
namespace Server.Services;

public class YandexStorageOptions
{
    public string KeyIdentifier { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public BucketOptions Buckets { get; set; } = new();

    public class BucketOptions
    {
        public string MainBucket   { get; set; } = string.Empty;
        public string ElibLogging  { get; set; } = string.Empty;
        public string PublicBaseUrl { get; set; } = string.Empty;
    }
}
