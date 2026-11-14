using System.Text;
using Pos.Application;
using Pos.Application.Ports;

namespace Pos.Infrastructure.Export;

/// <summary>
/// Каркас выгрузки (GoF: Template Method). Порядок шагов «подготовить строки -> сериализовать -> записать файл»
/// един для всех форматов; подклассы переопределяют только сериализацию
/// </summary>
public abstract class ResultExporterBase : IResultExporter
{
    public abstract string Format { get; }

    public void Export(IReadOnlyList<FinalGradeDto> rows, string path)
    {
        var content = Serialize(Prepare(rows));

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    /// <summary>Шаг по умолчанию: стабильный порядок по идентификатору студента. Можно переопределить</summary>
    protected virtual IReadOnlyList<FinalGradeDto> Prepare(IReadOnlyList<FinalGradeDto> rows) =>
        rows.OrderBy(r => r.StudentId).ToList();

    protected abstract string Serialize(IReadOnlyList<FinalGradeDto> rows);
}
