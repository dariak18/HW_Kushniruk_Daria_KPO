using System.Text.Encodings.Web;
using System.Text.Json;
using Pos.Application;

namespace Pos.Infrastructure.Export;

public sealed class JsonResultExporter : ResultExporterBase
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public override string Format => "json";

    protected override string Serialize(IReadOnlyList<FinalGradeDto> rows) =>
        JsonSerializer.Serialize(rows, Options);
}
