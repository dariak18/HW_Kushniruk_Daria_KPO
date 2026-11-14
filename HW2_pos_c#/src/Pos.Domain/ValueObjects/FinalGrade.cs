using Pos.Domain.Common;

namespace Pos.Domain.ValueObjects;

/// <summary>
/// Итог по студенту. Это производное значение: его нельзя поставить, только получить из оценок и рубрикатора
/// через <see cref="Services.FinalGradeCalculator"/>. Шкала итога — от 0 до <see cref="ScaleMax"/>.
/// </summary>
public sealed record FinalGrade
{
    public const decimal ScaleMax = 10m;

    internal FinalGrade(StudentId studentId, decimal value, int gradedCriteria, int totalCriteria)
    {
        if (value < 0 || value > ScaleMax)
        {
            throw new DomainException($"Итог должен лежать в диапазоне 0..{ScaleMax}, получено {value}.");
        }

        if (gradedCriteria < 0 || gradedCriteria > totalCriteria)
        {
            throw new DomainException("Количество оценённых критериев не может превышать общее число критериев.");
        }

        StudentId = studentId;
        Value = value;
        GradedCriteria = gradedCriteria;
        TotalCriteria = totalCriteria;
    }

    public StudentId StudentId { get; }

    public decimal Value { get; }

    public int GradedCriteria { get; }

    public int TotalCriteria { get; }
}
