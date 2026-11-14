using Pos.Domain.Model;
using Pos.Domain.ValueObjects;

namespace Pos.Domain.Factories;

/// <summary>
/// Фабрика оценок (GoF: порождающий паттерн). Единственная точка, где оценка создаётся с нуля:
/// проверяет связь с рубрикатором, строит Score и проставляет время. Вызывающий код не может
/// собрать оценку в обход этих проверок
/// </summary>
public sealed class EvaluationFactory
{
    private readonly TimeProvider _time;

    public EvaluationFactory(TimeProvider time)
    {
        _time = time;
    }

    public Evaluation Create(
        Rubric rubric,
        StudentId studentId,
        CriterionId criterionId,
        decimal rawScore,
        string? comment = null)
    {
        return new Evaluation(studentId, rubric, criterionId, new Score(rawScore), comment, _time.GetUtcNow().UtcDateTime);
    }
}
