using Pos.Domain.Grading;
using Pos.Domain.Model;
using Pos.Domain.ValueObjects;

namespace Pos.Domain.Services;

/// <summary>
/// Domain Service: расчёт итога. Правило затрагивает сразу рубрикатор (критерии, веса)
/// и оценки студента, поэтому не принадлежит ни одной сущности. Сам сервис не хранит состояния,
/// конкретная формула приходит как <see cref="IGradingStrategy"/>
/// </summary>
public sealed class FinalGradeCalculator
{
    public FinalGrade Calculate(
        StudentId studentId,
        Rubric rubric,
        IEnumerable<Evaluation> evaluations,
        IGradingStrategy strategy)
    {
        // Итог согласован с текущим набором оценок: учитываем только оценки этого студента
        // по критериям, которые сейчас есть в рубрикаторе
        var relevant = evaluations
            .Where(e => e.StudentId == studentId && rubric.Contains(e.CriterionId))
            .ToList();

        var value = strategy.Calculate(rubric, relevant);
        return new FinalGrade(studentId, value, relevant.Count, rubric.Criteria.Count);
    }
}
