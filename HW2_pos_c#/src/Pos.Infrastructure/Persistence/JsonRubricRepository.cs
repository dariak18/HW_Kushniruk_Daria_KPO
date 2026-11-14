using Pos.Domain.Model;
using Pos.Domain.Repositories;
using Pos.Domain.ValueObjects;

namespace Pos.Infrastructure.Persistence;

/// <summary>Адаптер порта <see cref="IRubricRepository"/>: критерии лежат в rubric.json</summary>
public sealed class JsonRubricRepository : IRubricRepository
{
    private sealed record Record(int Id, string Name, decimal MaxScore, decimal Weight);

    private readonly string _path;

    public JsonRubricRepository(StorageOptions options)
    {
        _path = Path.Combine(options.DataDirectory, "rubric.json");
    }

    public Rubric Get() =>
        Rubric.Restore(JsonFile.Load<Record>(_path).Select(r =>
            new Criterion(new CriterionId(r.Id), r.Name, new MaxScore(r.MaxScore), new Weight(r.Weight))));

    public void Save(Rubric rubric) =>
        JsonFile.Save(_path, rubric.Criteria.Select(c =>
            new Record(c.Id.Value, c.Name, c.MaxScore.Value, c.Weight.Value)));
}
