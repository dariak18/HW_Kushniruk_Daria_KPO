using Pos.Domain.Model;

namespace Pos.Domain.Grading;

/// <summary>
/// Округление поверх любого правила (GoF: Decorator). Позволяет добавлять «особенности подсчёта»
/// без переписывания самих правил и без комбинаторного взрыва подклассов
/// </summary>
public sealed class RoundingGradingStrategy : IGradingStrategy
{
    private readonly IGradingStrategy _inner;
    private readonly int _decimals;

    public RoundingGradingStrategy(IGradingStrategy inner, int decimals = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(decimals);
        _inner = inner;
        _decimals = decimals;
    }

    public string Name => $"{_inner.Name}, округление до {_decimals} зн.";

    public decimal Calculate(Rubric rubric, IReadOnlyCollection<Evaluation> evaluations) =>
        Math.Round(_inner.Calculate(rubric, evaluations), _decimals, MidpointRounding.AwayFromZero);
}
