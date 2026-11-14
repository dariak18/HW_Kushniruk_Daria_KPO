using Pos.Domain.Model;
using Pos.Domain.ValueObjects;

namespace Pos.Domain.Grading;

/// <summary>
/// Строгое правило: средневзвешенное по всему рубрикатору, неоценённый критерий считается нулём.
/// Пока студента не оценили по всем критериям, итог занижен — это сознательно
/// </summary>
public sealed class WeightedAverageStrategy : IGradingStrategy
{
    public string Name => "строгое средневзвешенное (нет оценки = 0)";

    public decimal Calculate(Rubric rubric, IReadOnlyCollection<Evaluation> evaluations)
    {
        var totalWeight = rubric.TotalWeight;
        if (totalWeight == 0)
        {
            return 0;
        }

        var byCriterion = evaluations.ToDictionary(e => e.CriterionId);
        var weighted = rubric.Criteria.Sum(c =>
            byCriterion.TryGetValue(c.Id, out var evaluation)
                ? c.Weight.Value * evaluation.Score.RatioTo(c.MaxScore)
                : 0m);

        return weighted / totalWeight * FinalGrade.ScaleMax;
    }
}
