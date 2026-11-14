using Pos.Domain.Common;

namespace Pos.Domain.ValueObjects;

/// <summary>Вклад критерия в итог. Всегда положителен</summary>
public sealed record Weight
{
    public Weight(decimal value)
    {
        if (value <= 0)
        {
            throw new DomainException("Вес критерия должен быть больше нуля.");
        }

        Value = value;
    }

    public decimal Value { get; }
}

/// <summary>Максимальный балл критерия. Всегда положителен</summary>
public sealed record MaxScore
{
    public MaxScore(decimal value)
    {
        if (value <= 0)
        {
            throw new DomainException("Максимальный балл критерия должен быть больше нуля.");
        }

        Value = value;
    }

    public decimal Value { get; }
}

/// <summary>Выставленный балл. Не бывает отрицательным; верхнюю границу проверяет оценка относительно критерия</summary>
public sealed record Score
{
    public Score(decimal value)
    {
        if (value < 0)
        {
            throw new DomainException("Балл не может быть отрицательным.");
        }

        Value = value;
    }

    public decimal Value { get; }

    /// <summary>Доля от максимума критерия, число от 0 до 1 (для корректной оценки)</summary>
    public decimal RatioTo(MaxScore max) => Value / max.Value;
}
