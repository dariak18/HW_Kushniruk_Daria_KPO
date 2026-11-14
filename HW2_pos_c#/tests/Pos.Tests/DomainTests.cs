using Pos.Domain.Common;
using Pos.Domain.Factories;
using Pos.Domain.Grading;
using Pos.Domain.Model;
using Pos.Domain.Services;
using Pos.Domain.ValueObjects;

namespace Pos.Tests;

public static class DomainTests
{
    private static readonly StudentId Ivan = new(1);

    public static IEnumerable<(string Name, Action Body)> All()
    {
        yield return ("Restore: балл выше максимума и неизвестный критерий отклоняются", () =>
        {
            var (rubric, factory) = Setup();
            Check.Throws<DomainException>(() => Evaluation.Restore(
                rubric, Ivan, new CriterionId(1), new Score(12), null, DateTime.UtcNow));
            Check.Throws<DomainException>(() => Evaluation.Restore(
                rubric, Ivan, new CriterionId(99), new Score(5), null, DateTime.UtcNow));
        });
        yield return ("Correct: удалённый критерий нельзя использовать", () =>
        {
            var (rubric, factory) = Setup();
            var evaluation = factory.Create(rubric, Ivan, new CriterionId(1), 5);
            rubric.RemoveCriterion(new CriterionId(1));
            Check.Throws<DomainException>(() => evaluation.Correct(rubric, new Score(6), null, DateTime.UtcNow));
            Check.Equal(5m, evaluation.Score.Value);
        });
        // Инварианты value object'ов
        yield return ("Weight: ноль и отрицательные значения запрещены", () =>
        {
            Check.Throws<DomainException>(() => new Weight(0));
            Check.Throws<DomainException>(() => new Weight(-1));
        }
        );
        yield return ("MaxScore: должен быть положительным", () =>
            Check.Throws<DomainException>(() => new MaxScore(0)));
        yield return ("Score: отрицательный балл запрещён", () =>
            Check.Throws<DomainException>(() => new Score(-0.1m)));

        // Инварианты рубрикатора и оценки
        yield return ("Rubric: нельзя добавить критерий с повторяющимся названием", () =>
        {
            var rubric = new Rubric();
            rubric.AddCriterion("Экзамен", 10, 1);
            Check.Throws<DomainException>(() => rubric.AddCriterion("  экзамен ", 5, 1));
        }
        );
        yield return ("Factory: балл выше максимума критерия отклоняется", () =>
        {
            var (rubric, factory) = Setup();
            Check.Throws<DomainException>(() => factory.Create(rubric, Ivan, new CriterionId(1), 10.5m));
        }
        );
        yield return ("Factory: критерий вне рубрикатора отклоняется", () =>
        {
            var (rubric, factory) = Setup();
            Check.Throws<DomainException>(() => factory.Create(rubric, Ivan, new CriterionId(99), 5));
        }
        );
        yield return ("Factory: граничные значения 0 и max допустимы", () =>
        {
            var (rubric, factory) = Setup();
            Check.Equal(0m, factory.Create(rubric, Ivan, new CriterionId(1), 0).Score.Value);
            Check.Equal(10m, factory.Create(rubric, Ivan, new CriterionId(1), 10).Score.Value);
        }
        );
        yield return ("Evaluation.Correct: исправленный балл тоже проверяется по критерию", () =>
        {
            var (rubric, factory) = Setup();
            var evaluation = factory.Create(rubric, Ivan, new CriterionId(1), 5);
            Check.Throws<DomainException>(() =>
                evaluation.Correct(rubric, new Score(11), null, DateTime.UtcNow));
            Check.Equal(5m, evaluation.Score.Value);
        }
        );

        // Domain Service и стратегии
        yield return ("Calculator: строгое правило считает неоценённый критерий нулём", () =>
        {
            var (rubric, factory) = Setup();
            var grade = new FinalGradeCalculator().Calculate(
                Ivan,
                rubric,
                new[] { factory.Create(rubric, Ivan, new CriterionId(1), 10) },
                new WeightedAverageStrategy());
            Check.Equal(3m, grade.Value); // вес 3 из 10 при полном балле
            Check.Equal(1, grade.GradedCriteria);
            Check.Equal(2, grade.TotalCriteria);
        }
        );
        yield return ("Calculator: мягкое правило нормирует по оценённым критериям", () =>
        {
            var (rubric, factory) = Setup();
            var grade = new FinalGradeCalculator().Calculate(
                Ivan,
                rubric,
                new[] { factory.Create(rubric, Ivan, new CriterionId(1), 10) },
                new GradedOnlyWeightedAverageStrategy());
            Check.Equal(10m, grade.Value);
        }
        );
        yield return ("Calculator: полный набор оценок даёт взвешенный итог", () =>
        {
            var (rubric, factory) = Setup();
            var grade = new FinalGradeCalculator().Calculate(
                Ivan,
                rubric,
                new[]
                {
                    factory.Create(rubric, Ivan, new CriterionId(1), 10),
                    factory.Create(rubric, Ivan, new CriterionId(2), 5),
                },
                new WeightedAverageStrategy());
            Check.Equal(6.5m, grade.Value); // (3*1 + 7*0.5) / 10 * 10
        }
        );
        yield return ("Calculator: чужие оценки и оценки удалённых критериев не влияют на итог", () =>
        {
            var (rubric, factory) = Setup();
            var other = new StudentId(2);
            var foreign = factory.Create(rubric, other, new CriterionId(1), 10);
            var removed = rubric.AddCriterion("Удаляемый", 10, 1);
            var orphan = factory.Create(rubric, Ivan, removed.Id, 10);
            rubric.RemoveCriterion(removed.Id);
            var grade = new FinalGradeCalculator().Calculate(
                Ivan, rubric, new[] { foreign, orphan }, new WeightedAverageStrategy());
            Check.Equal(0m, grade.Value);
            Check.Equal(0, grade.GradedCriteria);
        }
        );
        yield return ("Calculator: пустой рубрикатор даёт 0, а не деление на ноль", () =>
        {
            var grade = new FinalGradeCalculator().Calculate(
                Ivan, new Rubric(), Array.Empty<Evaluation>(), new WeightedAverageStrategy());
            Check.Equal(0m, grade.Value);
        }
        );
        yield return ("Decorator: округление применяется поверх любого правила", () =>
        {
            var rubric = new Rubric();
            rubric.AddCriterion("A", 3, 1);
            var factory = new EvaluationFactory(TimeProvider.System);
            var evaluation = factory.Create(rubric, Ivan, new CriterionId(1), 1); // 1/3 → 3.333...
            var rounded = new RoundingGradingStrategy(new WeightedAverageStrategy(), 1);
            Check.Equal(3.3m, rounded.Calculate(rubric, new[] { evaluation }));
        }
        );
        yield return ("FinalGrade: значение вне шкалы 0..10 невозможно", () =>
        {
            var calculator = new FinalGradeCalculator();
            Check.Throws<DomainException>(() => calculator.Calculate(
                Ivan, new Rubric(), Array.Empty<Evaluation>(), new InvalidStrategy(10.01m)));
            Check.Throws<DomainException>(() => calculator.Calculate(
                Ivan, new Rubric(), Array.Empty<Evaluation>(), new InvalidStrategy(-1m)));
        }
        );
    }

    private sealed class InvalidStrategy(decimal value) : IGradingStrategy
    {
        public string Name => "Некорректная стратегия для теста";
        public decimal Calculate(Rubric rubric, IReadOnlyCollection<Evaluation> evaluations) => value;
    }

    private static (Rubric Rubric, EvaluationFactory Factory) Setup()
    {
        var rubric = new Rubric();
        rubric.AddCriterion("Домашние задания", 10, 3);
        rubric.AddCriterion("Экзамен", 10, 7);
        return (rubric, new EvaluationFactory(TimeProvider.System));
    }
}
