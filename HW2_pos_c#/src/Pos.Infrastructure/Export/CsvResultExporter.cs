using System.Globalization;
using System.Text;
using Pos.Application;

namespace Pos.Infrastructure.Export;

public sealed class CsvResultExporter : ResultExporterBase
{
    public override string Format => "csv";

    protected override string Serialize(IReadOnlyList<FinalGradeDto> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("student_id,student,final,graded,total,rule");
        builder.AppendLine();

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(
                ',',
                row.StudentId.ToString(CultureInfo.InvariantCulture),
                Escape(row.StudentName),
                row.Value.ToString(CultureInfo.InvariantCulture),
                row.GradedCriteria.ToString(CultureInfo.InvariantCulture),
                row.TotalCriteria.ToString(CultureInfo.InvariantCulture),
                Escape(row.Rule)));

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string Escape(string value) =>
        value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
}
