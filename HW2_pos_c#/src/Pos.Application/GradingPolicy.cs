using Pos.Domain.Grading;

namespace Pos.Application;

public enum GradingMode
{
    Strict,
    Lenient,
}

/// <summary>
/// Текущая настройка расчёта итога. Собирает стратегию из базового правила и,
/// при необходимости, оборачивает её декоратором округления
/// </summary>
public sealed class GradingPolicy
{
    public GradingMode Mode { get; set; } = GradingMode.Strict;

    public bool RoundResult { get; set; } = true;

    public int Decimals { get; set; } = 1;

    public IGradingStrategy Build()
    {
        IGradingStrategy strategy = Mode == GradingMode.Strict
            ? new WeightedAverageStrategy()
            : new GradedOnlyWeightedAverageStrategy();

        return RoundResult ? new RoundingGradingStrategy(strategy, Decimals) : strategy;
    }
}
