using System.ComponentModel;
using Microsoft.Extensions.AI;
using OpenChatAgents.Domain.Abstractions;

namespace OpenChatAgents.Infrastructure.BuiltInTools;

/// <summary>Built-in "data e hora" tool group — current time and timezone conversion, no external state.</summary>
public class DateTimeToolProvider : IBuiltInToolProvider
{
    public string Key => "datetime";

    public IReadOnlyList<AITool> GetTools(BuiltInToolContext context) =>
    [
        AIFunctionFactory.Create(GetCurrentTime, new AIFunctionFactoryOptions
        {
            Name = "get_current_time",
            Description = "Retorna a data e hora atuais num fuso horário (IANA, ex: 'America/Sao_Paulo'); usa UTC se omitido.",
        }),
        AIFunctionFactory.Create(ConvertTime, new AIFunctionFactoryOptions
        {
            Name = "convert_time",
            Description = "Converte um horário (ISO 8601) de um fuso horário IANA para outro.",
        }),
        AIFunctionFactory.Create(ListTimezones, new AIFunctionFactoryOptions
        {
            Name = "list_timezones",
            Description = "Lista os identificadores de fuso horário IANA disponíveis.",
        }),
    ];

    private string GetCurrentTime([Description("Fuso horário IANA, ex: 'America/Sao_Paulo'; vazio para UTC")] string timezone = "")
    {
        var tz = ResolveTimeZone(timezone);
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
        return $"{now:yyyy-MM-dd HH:mm:ss zzz} ({tz.Id})";
    }

    private string ConvertTime(
        [Description("Horário em formato ISO 8601, ex: '2026-01-01T12:00:00'")] string time,
        [Description("Fuso horário IANA de origem")] string fromTimezone,
        [Description("Fuso horário IANA de destino")] string toTimezone)
    {
        if (!DateTimeOffset.TryParse(time, out var parsed))
            return $"Não consegui interpretar o horário '{time}'.";

        var from = ResolveTimeZone(fromTimezone);
        var to = ResolveTimeZone(toTimezone);
        var sourceUtc = new DateTimeOffset(DateTime.SpecifyKind(parsed.DateTime, DateTimeKind.Unspecified), from.GetUtcOffset(parsed.DateTime));
        var converted = TimeZoneInfo.ConvertTime(sourceUtc, to);
        return $"{converted:yyyy-MM-dd HH:mm:ss zzz} ({to.Id})";
    }

    private string ListTimezones() => string.Join("\n", TimeZoneInfo.GetSystemTimeZones().Select(tz => tz.Id));

    private static TimeZoneInfo ResolveTimeZone(string timezone)
    {
        if (string.IsNullOrWhiteSpace(timezone))
            return TimeZoneInfo.Utc;
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezone);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
