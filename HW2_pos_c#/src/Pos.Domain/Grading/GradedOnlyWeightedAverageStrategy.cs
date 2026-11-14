using Pos.Domain.Model;
using Pos.Domain.ValueObjects;

namespace Pos.Domain.Grading;

/// <summary>
/// Мягкое правило: средневзвешенное только по тем критериям, по которым уже есть оценка.
/// Подходит для промежуточного итога посреди семестра
/// </summary>
public sealed class GradedOnlyWeightedAverageStrategy : IGradingStrategy
{
    public string Name => "средневзвешенное по оценённым критериям";

    public decimal Calculate(Rubric rubric, IReadOnlyCollection<Evaluation> evaluations)
    {
        var byCriterion = evaluations.ToDictionary(e => e.CriterionId);
        var graded = rubric.Criteria.Where(c => byCriterion.ContainsKey(c.Id)).ToList();
        var gradedWeight = graded.Sum(c => c.Weight.Value);
        if (gradedWeight == 0)
        {
            return 0;
        }

        var weighted = graded.Sum(c => c.Weight.Value * byCriterion[c.Id].Score.RatioTo(c.MaxScore));
        return weighted / gradedWeight * FinalGrade.ScaleMax;
    }
}
