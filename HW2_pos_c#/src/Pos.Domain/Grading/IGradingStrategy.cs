using Pos.Domain.Model;

namespace Pos.Domain.Grading;

/// <summary>
/// Правило расчёта итога (GoF: Strategy). Возвращает значение по шкале 0..<see cref="ValueObjects.FinalGrade.ScaleMax"/>
/// </summary>
public interface IGradingStrategy
{
    string Name { get; }

    decimal Calculate(Rubric rubric, IReadOnlyCollection<Evaluation> evaluations);
}
