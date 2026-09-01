namespace OpenChatAgents.Domain.Abstractions;

public interface ITextExtractor
{
    string Extract(Stream stream, string fileName);
}
