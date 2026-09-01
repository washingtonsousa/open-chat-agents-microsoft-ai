namespace OpenChatAgents.Domain.Abstractions;

public interface IObjectStore
{
    Task EnsureBucketExistsAsync(CancellationToken cancellationToken = default);
    Task PutObjectAsync(string objectKey, Stream data, long size, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> GetObjectStreamAsync(string objectKey, CancellationToken cancellationToken = default);
    Task RemoveObjectAsync(string objectKey, CancellationToken cancellationToken = default);
    Task RemovePrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
