using System.Text.RegularExpressions;

namespace OpenChatAgents.Api.Services;

public partial class ModerationService
{
    private const string ViolationResponse =
        "Desculpe, não consigo responder mensagens com linguagem inapropriada. " +
        "Por favor, reformule sua pergunta de forma respeitosa.";

    public bool ContainsProfanity(string text) => ProfanityRegex().IsMatch(text);

    public string GetViolationResponse() => ViolationResponse;

    [GeneratedRegex(
        @"\bputa\b|\bmerda\b|\bporra\b|\bcaralho\b|\bviado\b|\bbostinha\b|\bbosta\b|\bfilha?\s*da\s*puta\b|" +
        @"\bvadia\b|\bputa\s*que\s*pariu\b|\bidiota\b|\bestupido\b|\bcretino\b|\bimbecil\b|\botario\b|" +
        @"\bbabaca\b|\bartificial\b|\bshit\b|\bfuck\b|\bass\b|\bbitch\b|\bdamn\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex ProfanityRegex();
}
