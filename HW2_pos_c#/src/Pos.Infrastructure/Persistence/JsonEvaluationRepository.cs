using Pos.Domain.Model;
using Pos.Domain.Repositories;
using Pos.Domain.ValueObjects;

namespace Pos.Infrastructure.Persistence;

/// <summary>Адаптер порта <see cref="IEvaluationRepository"/>: оценки лежат в evaluations.json</summary>
public sealed class JsonEvaluationRepository : IEvaluationRepository
{
    private sealed record Record(
        int StudentId,
        int CriterionId,
        decimal Score,
        string? Comment,
        DateTime EvaluatedAtUtc);

    private readonly string _path;
    private readonly Dictionary<EvaluationId, Evaluation> _items;

    public JsonEvaluationRepository(StorageOptions options, IRubricRepository rubrics)
    {
        _path = Path.Combine(options.DataDirectory, "evaluations.json");
        var rubric = rubrics.Get();
        _items = JsonFile.Load<Record>(_path)
            .Select(r => Evaluation.Restore(
                rubric,
                new StudentId(r.StudentId),
                new CriterionId(r.CriterionId),
                new Score(r.Score),
                r.Comment,
                r.EvaluatedAtUtc))
            .ToDictionary(e => e.Id);
    }

    public Evaluation? Find(StudentId studentId, CriterionId criterionId) =>
        _items.GetValueOrDefault(new EvaluationId(studentId, criterionId));

    public IReadOnlyList<Evaluation> GetByStudent(StudentId studentId) =>
        _items.Values.Where(e => e.StudentId == studentId).ToList();

    public IReadOnlyList<Evaluation> GetAll() => _items.Values.ToList();

    public void Save(Evaluation evaluation)
    {
        _items[evaluation.Id] = evaluation;
        Persist();
    }

    public void Remove(StudentId studentId, CriterionId criterionId)
    {
        if (_items.Remove(new EvaluationId(studentId, criterionId)))
        {
            Persist();
        }
    }

    public void RemoveByStudent(StudentId studentId) => RemoveWhere(e => e.StudentId == studentId);

    public void RemoveByCriterion(CriterionId criterionId) => RemoveWhere(e => e.CriterionId == criterionId);

    private void RemoveWhere(Func<Evaluation, bool> predicate)
    {
        var ids = _items.Values.Where(predicate).Select(e => e.Id).ToList();
        foreach (var id in ids)
        {
            _items.Remove(id);
        }

        if (ids.Count > 0)
        {
            Persist();
        }
    }

    private void Persist() =>
        JsonFile.Save(
            _path,
            _items.Values
                .OrderBy(e => e.StudentId.Value)
                .ThenBy(e => e.CriterionId.Value)
                .Select(e => new Record(
                    e.StudentId.Value,
                    e.CriterionId.Value,
                    e.Score.Value,
                    e.Comment,
                    e.EvaluatedAtUtc)));
}
