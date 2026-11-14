using Pos.Domain.Common;
using Pos.Domain.ValueObjects;

namespace Pos.Domain.Model;

/// <summary>
/// Оценка — факт «этому студенту по этому критерию поставили такой балл».
/// Инвариант «0 <= балл <= максимум критерия» охраняется самой сущностью: и при создании, и при исправлении
/// </summary>
public sealed class Evaluation : Entity<EvaluationId>
{
    internal Evaluation(StudentId studentId, Rubric rubric, CriterionId criterionId, Score score, string? comment, DateTime evaluatedAtUtc)
        : base(new EvaluationId(studentId, criterionId))
    {
        var criterion = rubric.Get(criterionId);
        EnsureWithinRange(criterion, score);
        Score = score;
        Comment = NormalizeComment(comment);
        EvaluatedAtUtc = evaluatedAtUtc;
    }

    public StudentId StudentId => Id.StudentId;

    public CriterionId CriterionId => Id.CriterionId;

    public Score Score { get; private set; }

    public string? Comment { get; private set; }

    public DateTime EvaluatedAtUtc { get; private set; }

    /// <summary>
    /// Восстановление из хранилища с повторной проверкой критерия и диапазона балла
    /// </summary>
    public static Evaluation Restore(
        Rubric rubric,
        StudentId studentId,
        CriterionId criterionId,
        Score score,
        string? comment,
        DateTime evaluatedAtUtc) =>
        new(studentId, rubric, criterionId, score, comment, evaluatedAtUtc);

    /// <summary>Исправление оценки. Новый балл проверяется по тому же критерию</summary>
    public void Correct(Rubric rubric, Score newScore, string? comment, DateTime evaluatedAtUtc)
    {
        var criterion = rubric.Get(CriterionId);

        EnsureWithinRange(criterion, newScore);
        Score = newScore;
        Comment = NormalizeComment(comment);
        EvaluatedAtUtc = evaluatedAtUtc;
    }

    private static void EnsureWithinRange(Criterion criterion, Score score)
    {
        if (score.Value > criterion.MaxScore.Value)
        {
            throw new DomainException(
                $"Балл {score.Value} выходит за пределы критерия «{criterion.Name}» (0..{criterion.MaxScore.Value}).");
        }
    }

    private static string? NormalizeComment(string? comment) =>
        string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
}
