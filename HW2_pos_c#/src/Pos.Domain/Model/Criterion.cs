using Pos.Domain.Common;
using Pos.Domain.ValueObjects;

namespace Pos.Domain.Model;

/// <summary>Критерий оценивания: за что ставят балл, какой максимум и какой вклад в итог</summary>
public sealed class Criterion : Entity<CriterionId>
{
    public Criterion(CriterionId id, string name, MaxScore maxScore, Weight weight) : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Название критерия не может быть пустым.");
        }

        Name = name.Trim();
        MaxScore = maxScore;
        Weight = weight;
    }

    public string Name { get; }

    public MaxScore MaxScore { get; }

    public Weight Weight { get; }
}
