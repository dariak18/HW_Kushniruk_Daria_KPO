using Pos.Application;
using Pos.Application.Ports;
using Pos.Application.UseCases;
using Pos.Domain.Common;
using Pos.Domain.Factories;
using Pos.Domain.Services;
using Pos.Infrastructure.Export;
using Pos.Infrastructure.Persistence;

namespace Pos.Tests;

/// <summary>Сценарии приложения и адаптеры. Для изоляции каждый тест работает в своём временном каталоге</summary>
public static class ApplicationTests
{
    public static IEnumerable<(string Name, Action Body)> All()
    {
        yield return ("Repository: некорректные оценки в JSON отклоняются при загрузке", () =>
        {
            using var env = new TestEnvironment();
            env.Pos.AddStudent("Студент");
            env.Pos.AddCriterion("Экзамен", 10, 1);
            var path = Path.Combine(env.Directory, "evaluations.json");
            File.WriteAllText(path, """[{"StudentId":1,"CriterionId":1,"Score":12}]""");
            Check.Throws<DomainException>(() => env.CreateFacade());
            File.WriteAllText(path, """[{"StudentId":1,"CriterionId":99,"Score":5}]""");
            Check.Throws<DomainException>(() => env.CreateFacade());
        });
        yield return ("UseCase: нельзя выставить оценку несуществующему студенту", () =>
        {
            using var env = new TestEnvironment();
            env.Pos.AddCriterion("Экзамен", 10, 1);
            Check.Throws<NotFoundException>(() => env.Pos.SetGrade(5, 1, 5));
        }
        );
        yield return ("UseCase: повторная оценка по критерию исправляет, а не дублирует", () =>
        {
            using var env = new TestEnvironment();
            var student = env.Pos.AddStudent("Кушнирук Дарья");
            env.Pos.AddCriterion("Экзамен", 10, 1);
            Check.True(!env.Pos.SetGrade(student.Id, 1, 4).WasCorrection, "первая оценка не должна быть исправлением");
            Check.True(env.Pos.SetGrade(student.Id, 1, 7).WasCorrection, "вторая оценка должна быть исправлением");
            Check.Equal(1, env.Pos.ListGrades(student.Id).Count);
            Check.Equal(7m, env.Pos.ListGrades(student.Id)[0].Score);
        }
        );
        yield return ("UseCase: неудачное исправление не портит прежнюю оценку", () =>
        {
            using var env = new TestEnvironment();
            var student = env.Pos.AddStudent("Кушнирук Дарья");
            env.Pos.AddCriterion("Экзамен", 10, 1);
            env.Pos.SetGrade(student.Id, 1, 6);
            Check.Throws<DomainException>(() => env.Pos.SetGrade(student.Id, 1, 11));
            Check.Equal(6m, env.Pos.ListGrades(student.Id)[0].Score);
        }
        );
        yield return ("UseCase: удаление критерия убирает оценки по нему", () =>
        {
            using var env = new TestEnvironment();
            var student = env.Pos.AddStudent("Кушнирук Дарья");
            env.Pos.AddCriterion("Экзамен", 10, 1);
            env.Pos.AddCriterion("Домашка", 10, 1);
            env.Pos.SetGrade(student.Id, 1, 6);
            env.Pos.SetGrade(student.Id, 2, 8);
            env.Pos.RemoveCriterion(1);
            Check.Equal(1, env.Pos.ListGrades(student.Id).Count);
            Check.Equal("Домашка", env.Pos.ListGrades(student.Id)[0].CriterionName);
        }
        );
        yield return ("UseCase: итог пересчитывается после отмены оценки", () =>
        {
            using var env = new TestEnvironment();
            var student = env.Pos.AddStudent("Кушнирук Дарья");
            env.Pos.AddCriterion("Экзамен", 10, 1);
            env.Pos.SetGrade(student.Id, 1, 10);
            Check.Equal(10m, env.Pos.GetResult(student.Id).Value);
            env.Pos.CancelGrade(student.Id, 1);
            Check.Equal(0m, env.Pos.GetResult(student.Id).Value);
        }
        );
        yield return ("Repository: данные переживают перезапуск (JSON)", () =>
        {
            using var env = new TestEnvironment();
            var student = env.Pos.AddStudent("Кушнирук Дарья");
            env.Pos.AddCriterion("Экзамен", 10, 2);
            env.Pos.SetGrade(student.Id, 1, 8, "хорошо");

            var restarted = env.CreateFacade(); // новые репозитории читают те же файлы
            Check.Equal("Кушнирук Дарья", restarted.ListStudents()[0].FullName);
            Check.Equal(2m, restarted.ListCriteria()[0].Weight);
            Check.Equal("хорошо", restarted.ListGrades(student.Id)[0].Comment);
        }
        );
        yield return ("Export: CSV и JSON создаются, неизвестный формат — ошибка", () =>
        {
            using var env = new TestEnvironment();
            var student = env.Pos.AddStudent("Кушнирук, Дарья");
            env.Pos.AddCriterion("Экзамен", 10, 1);
            env.Pos.SetGrade(student.Id, 1, 9);

            var csvPath = Path.Combine(env.Directory, "out", "r.csv");
            var jsonPath = Path.Combine(env.Directory, "out", "r.json");
            env.Pos.Export("csv", csvPath);
            env.Pos.Export("json", jsonPath);

            Check.True(File.ReadAllText(csvPath).Contains("\"Кушнирук, Дарья\""), "CSV должен экранировать запятую в имени");
            Check.True(File.ReadAllText(jsonPath).Contains("\"studentName\": \"Кушнирук, Дарья\""), "JSON должен содержать имя");
            Check.Throws<UseCaseException>(() => env.Pos.Export("xml", Path.Combine(env.Directory, "r.xml")));
        }
        );
    }

    private sealed class TestEnvironment : IDisposable
    {
        public TestEnvironment()
        {
            Directory = Path.Combine(Path.GetTempPath(), "pos-tests-" + Guid.NewGuid().ToString("N"));
            Pos = CreateFacade();
        }

        public string Directory { get; }

        public PosFacade Pos { get; }

        /// <summary>Ручная сборка графа без DI-контейнера: ядро от него не зависит</summary>
        public PosFacade CreateFacade()
        {
            var options = new StorageOptions(Directory);
            var students = new JsonStudentRepository(options);
            var rubrics = new JsonRubricRepository(options);
            var evaluations = new JsonEvaluationRepository(options, rubrics);
            var time = TimeProvider.System;
            var exporters = new IResultExporter[] { new CsvResultExporter(), new JsonResultExporter() };

            return new PosFacade(
                new StudentUseCases(students, evaluations),
                new RubricUseCases(rubrics, evaluations),
                new EvaluationUseCases(students, rubrics, evaluations, new EvaluationFactory(time), time),
                new ResultsUseCases(
                    students, rubrics, evaluations, new FinalGradeCalculator(), new GradingPolicy(), exporters));
        }

        public void Dispose()
        {
            if (System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.Delete(Directory, recursive: true);
            }
        }
    }
}
