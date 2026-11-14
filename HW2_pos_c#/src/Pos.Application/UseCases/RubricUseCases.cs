using Pos.Domain.Model;
using Pos.Domain.Repositories;
using Pos.Domain.ValueObjects;

namespace Pos.Application.UseCases;

public sealed class RubricUseCases
{
    private readonly IRubricRepository _rubrics;
    private readonly IEvaluationRepository _evaluations;

    public RubricUseCases(IRubricRepository rubrics, IEvaluationRepository evaluations)
    {
        _rubrics = rubrics;
        _evaluations = evaluations;
    }

    public CriterionDto AddCriterion(string name, decimal maxScore, decimal weight)
    {
        var rubric = _rubrics.Get();
        var criterion = rubric.AddCriterion(name, maxScore, weight);
        _rubrics.Save(rubric);
        return ToDto(criterion);
    }

    public IReadOnlyList<CriterionDto> List() =>
        _rubrics.Get().Criteria.Select(ToDto).ToList();

    /// <summary>Удаляет критерий вместе с оценками по нему, чтобы не оставить висячих оценок.</summary>
    public void Remove(int id)
    {
        var criterionId = new CriterionId(id);
        var rubric = _rubrics.Get();
        if (!rubric.Contains(criterionId))
        {
            throw new NotFoundException($"Критерий #{id} не найден.");
        }

        rubric.RemoveCriterion(criterionId);
        _rubrics.Save(rubric);
        _evaluations.RemoveByCriterion(criterionId);
    }

    private static CriterionDto ToDto(Criterion c) =>
        new(c.Id.Value, c.Name, c.MaxScore.Value, c.Weight.Value);
}
