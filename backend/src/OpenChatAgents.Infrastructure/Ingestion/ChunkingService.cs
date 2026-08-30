namespace OpenChatAgents.Infrastructure.Ingestion;

public record TextChunk(int Index, string Content, int CharStart, int CharEnd);

public static class ChunkingService
{
    public static List<TextChunk> Chunk(string text, int chunkSize, int chunkOverlap)
    {
        var chunks = new List<TextChunk>();
        if (string.IsNullOrWhiteSpace(text))
            return chunks;

        var step = Math.Max(1, chunkSize - chunkOverlap);
        var index = 0;
        var start = 0;

        while (start < text.Length)
        {
            var length = Math.Min(chunkSize, text.Length - start);
            var content = text.Substring(start, length).Trim();

            if (content.Length > 0)
                chunks.Add(new TextChunk(index++, content, start, start + length));

            if (start + length >= text.Length)
                break;

            start += step;
        }

        return chunks;
    }
}
