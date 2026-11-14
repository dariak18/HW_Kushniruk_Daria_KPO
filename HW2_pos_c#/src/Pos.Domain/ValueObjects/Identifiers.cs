using System.Globalization;
using Pos.Domain.Common;

namespace Pos.Domain.ValueObjects;

/// <summary>Идентификатор студента</summary>
public readonly record struct StudentId
{
    public StudentId(int value)
    {
        if (value <= 0)
        {
            throw new DomainException("Идентификатор студента должен быть положительным числом.");
        }

        Value = value;
    }

    public int Value { get; }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>Идентификатор критерия в рубрикаторе</summary>
public readonly record struct CriterionId
{
    public CriterionId(int value)
    {
        if (value <= 0)
        {
            throw new DomainException("Идентификатор критерия должен быть положительным числом.");
        }

        Value = value;
    }

    public int Value { get; }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>
/// Идентичность оценки: «этому студенту по этому критерию». Одна пара — одна оценка;
/// повторное выставление означает исправление, а не дубликат
/// </summary>
public readonly record struct EvaluationId(StudentId StudentId, CriterionId CriterionId);
