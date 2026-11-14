namespace Pos.Application.Ports;

/// <summary>Порт выгрузки итогов. Формат и способ записи — забота адаптера</summary>
public interface IResultExporter
{
    /// <summary>Короткое имя формата, например «csv» или «json»</summary>
    string Format { get; }

    void Export(IReadOnlyList<FinalGradeDto> rows, string path);
}
