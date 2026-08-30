using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using OpenChatAgents.Infrastructure.Options;

namespace OpenChatAgents.Infrastructure.Storage;

public class MinioObjectStore
{
    private readonly IMinioClient _client;
    private readonly string _bucket;

    public MinioObjectStore(IOptions<AppOptions> options)
    {
        var minioOptions = options.Value.Minio;
        _bucket = minioOptions.Bucket;
        _client = new MinioClient()
            .WithEndpoint(minioOptions.Endpoint)
            .WithCredentials(minioOptions.AccessKey, minioOptions.SecretKey)
            .WithSSL(minioOptions.UseSsl)
            .Build();
    }

    public async Task EnsureBucketExistsAsync(CancellationToken cancellationToken = default)
    {
        var exists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucket), cancellationToken);
        if (!exists)
            await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucket), cancellationToken);
    }

    public async Task PutObjectAsync(string objectKey, Stream data, long size, string contentType, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);
        await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectKey)
            .WithStreamData(data)
            .WithObjectSize(size)
            .WithContentType(contentType), cancellationToken);
    }

    public async Task<Stream> GetObjectStreamAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var memoryStream = new MemoryStream();
        await _client.GetObjectAsync(new GetObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectKey)
            .WithCallbackStream(stream => stream.CopyTo(memoryStream)), cancellationToken);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task RemoveObjectAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        await _client.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(_bucket).WithObject(objectKey), cancellationToken);
    }

    public async Task RemovePrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var listArgs = new ListObjectsArgs().WithBucket(_bucket).WithPrefix(prefix).WithRecursive(true);
        await foreach (var item in _client.ListObjectsEnumAsync(listArgs, cancellationToken))
        {
            await RemoveObjectAsync(item.Key, cancellationToken);
        }
    }
}
