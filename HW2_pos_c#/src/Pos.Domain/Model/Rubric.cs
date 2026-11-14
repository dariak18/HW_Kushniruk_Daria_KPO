using Pos.Domain.Common;
using Pos.Domain.ValueObjects;

namespace Pos.Domain.Model;

/// <summary>
/// Рубрикатор — корень агрегата, владеет набором критериев. Он единственный решает,
/// какие критерии «существуют», поэтому оценку по несуществующему критерию создать нельзя
/// </summary>
public sealed class Rubric
{
    private readonly Dictionary<CriterionId, Criterion> _criteria = new();

    public IReadOnlyList<Criterion> Criteria =>
        _criteria.Values.OrderBy(c => c.Id.Value).ToList();

    public decimal TotalWeight => _criteria.Values.Sum(c => c.Weight.Value);

    /// <summary>Восстановление агрегата из хранилища</summary>
    public static Rubric Restore(IEnumerable<Criterion> criteria)
    {
        var rubric = new Rubric();
        foreach (var criterion in criteria)
        {
            rubric.EnsureNameIsUnique(criterion.Name);
            rubric._criteria.Add(criterion.Id, criterion);
        }

        return rubric;
    }

    public Criterion AddCriterion(string name, decimal maxScore, decimal weight)
    {
        var criterion = new Criterion(NextId(), name, new MaxScore(maxScore), new Weight(weight));
        EnsureNameIsUnique(criterion.Name);
        _criteria.Add(criterion.Id, criterion);
        return criterion;
    }

    public void RemoveCriterion(CriterionId id)
    {
        if (!_criteria.Remove(id))
        {
            throw new DomainException($"Критерий #{id} отсутствует в рубрикаторе.");
        }
    }

    public bool Contains(CriterionId id) => _criteria.ContainsKey(id);

    /// <summary>Возвращает критерий или нарушает инвариант «только критерии из рубрикатора»</summary>
    public Criterion Get(CriterionId id) =>
        _criteria.TryGetValue(id, out var criterion)
            ? criterion
            : throw new DomainException($"Критерий #{id} отсутствует в рубрикаторе.");

    private CriterionId NextId() =>
        new(_criteria.Count == 0 ? 1 : _criteria.Keys.Max(k => k.Value) + 1);

    private void EnsureNameIsUnique(string name)
    {
        if (_criteria.Values.Any(c => string.Equals(c.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException($"Критерий «{name.Trim()}» уже есть в рубрикаторе.");
        }
    }
}
