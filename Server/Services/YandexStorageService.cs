// Storage service implementation using the AWS S3 SDK to interact with Yandex Object Storage.
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;


namespace Server.Services;

public class YandexStorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly YandexStorageOptions.BucketOptions _bucket;

    public YandexStorageService(IAmazonS3 s3, IOptions<YandexStorageOptions> options)
    {
        _s3 = s3;
        _bucket = options.Value.Buckets;
    }
    public async Task<string> GetFile(string storageKey)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket.MainBucket,
            Key = storageKey,
            Expires = DateTime.UtcNow.AddHours(24),
            Verb = HttpVerb.GET
        };
        return await Task.FromResult(_s3.GetPreSignedURL(request));
    }

    public async Task<string> GetUploadUrl(string storageKey, string contentType)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName  = _bucket.MainBucket,
            Key         = storageKey,
            Expires     = DateTime.UtcNow.AddMinutes(30),
            Verb        = HttpVerb.PUT,
            ContentType = contentType
        };
        return await Task.FromResult(_s3.GetPreSignedURL(request));
    }

    public async Task<string> UploadFile(string storageKey, Stream content, string contentType)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucket.MainBucket,
            Key = storageKey,
            InputStream = content,
            ContentType = contentType
        };
        await _s3.PutObjectAsync(request);
        return storageKey;
    }

    public async Task DeleteFile(string storageKey)
    {
        await _s3.DeleteObjectAsync(_bucket.MainBucket, storageKey);
    }
}
