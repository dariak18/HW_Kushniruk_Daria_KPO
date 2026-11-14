using Pos.Domain.Factories;
using Pos.Domain.Model;
using Pos.Domain.Repositories;
using Pos.Domain.ValueObjects;

namespace Pos.Application.UseCases;

public sealed class EvaluationUseCases
{
    private readonly IStudentRepository _students;
    private readonly IRubricRepository _rubrics;
    private readonly IEvaluationRepository _evaluations;
    private readonly EvaluationFactory _factory;
    private readonly TimeProvider _time;

    public EvaluationUseCases(
        IStudentRepository students,
        IRubricRepository rubrics,
        IEvaluationRepository evaluations,
        EvaluationFactory factory,
        TimeProvider time)
    {
        _students = students;
        _rubrics = rubrics;
        _evaluations = evaluations;
        _factory = factory;
        _time = time;
    }

    /// <summary>Выставляет оценку; если по этому критерию она уже есть — исправляет её</summary>
    public SetGradeResult Set(int studentId, int criterionId, decimal score, string? comment)
    {
        var sid = EnsureStudent(studentId);
        var cid = new CriterionId(criterionId);
        var rubric = _rubrics.Get();

        var existing = _evaluations.Find(sid, cid);
        Evaluation evaluation;
        if (existing is null)
        {
            evaluation = _factory.Create(rubric, sid, cid, score, comment);
        }
        else
        {
            existing.Correct(rubric, new Score(score), comment, _time.GetUtcNow().UtcDateTime);
            evaluation = existing;
        }

        _evaluations.Save(evaluation);
        return new SetGradeResult(ToDto(evaluation, rubric.Get(cid)), existing is not null);
    }

    public void Cancel(int studentId, int criterionId)
    {
        var sid = EnsureStudent(studentId);
        var cid = new CriterionId(criterionId);
        if (_evaluations.Find(sid, cid) is null)
        {
            throw new NotFoundException($"Оценки студента #{studentId} по критерию #{criterionId} нет.");
        }

        _evaluations.Remove(sid, cid);
    }

    public IReadOnlyList<EvaluationDto> List(int studentId)
    {
        var sid = EnsureStudent(studentId);
        var rubric = _rubrics.Get();
        return _evaluations.GetByStudent(sid)
            .Where(e => rubric.Contains(e.CriterionId))
            .OrderBy(e => e.CriterionId.Value)
            .Select(e => ToDto(e, rubric.Get(e.CriterionId)))
            .ToList();
    }

    private StudentId EnsureStudent(int studentId)
    {
        var id = new StudentId(studentId);
        return _students.Find(id) is null
            ? throw new NotFoundException($"Студент #{studentId} не найден.")
            : id;
    }

    private static EvaluationDto ToDto(Evaluation e, Criterion c) =>
        new(e.StudentId.Value, c.Id.Value, c.Name, e.Score.Value, c.MaxScore.Value, e.Comment, e.EvaluatedAtUtc);
}
