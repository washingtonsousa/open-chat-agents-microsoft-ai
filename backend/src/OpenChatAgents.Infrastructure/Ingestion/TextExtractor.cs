using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OpenChatAgents.Domain.Abstractions;
using UglyToad.PdfPig;

namespace OpenChatAgents.Infrastructure.Ingestion;

public class TextExtractor : ITextExtractor
{
    public string Extract(Stream stream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => ExtractPdf(stream),
            ".docx" => ExtractDocx(stream),
            _ => ExtractPlainText(stream),
        };
    }

    private static string ExtractPdf(Stream stream)
    {
        using var document = PdfDocument.Open(stream);
        return string.Join("\n\n", document.GetPages().Select(p => p.Text));
    }

    private static string ExtractDocx(Stream stream)
    {
        using var document = WordprocessingDocument.Open(stream, isEditable: false);
        var body = document.MainDocumentPart?.Document.Body;
        if (body is null)
            return string.Empty;

        var paragraphs = body.Descendants<Paragraph>().Select(p => p.InnerText);
        return string.Join("\n\n", paragraphs.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static string ExtractPlainText(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
