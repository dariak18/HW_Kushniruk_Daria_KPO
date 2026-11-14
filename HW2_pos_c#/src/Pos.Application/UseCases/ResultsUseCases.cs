using Pos.Application.Ports;
using Pos.Domain.Model;
using Pos.Domain.Repositories;
using Pos.Domain.Services;
using Pos.Domain.ValueObjects;

namespace Pos.Application.UseCases;

public sealed class ResultsUseCases
{
    private readonly IStudentRepository _students;
    private readonly IRubricRepository _rubrics;
    private readonly IEvaluationRepository _evaluations;
    private readonly FinalGradeCalculator _calculator;
    private readonly GradingPolicy _policy;
    private readonly IReadOnlyList<IResultExporter> _exporters;

    public ResultsUseCases(
        IStudentRepository students,
        IRubricRepository rubrics,
        IEvaluationRepository evaluations,
        FinalGradeCalculator calculator,
        GradingPolicy policy,
        IEnumerable<IResultExporter> exporters)
    {
        _students = students;
        _rubrics = rubrics;
        _evaluations = evaluations;
        _calculator = calculator;
        _policy = policy;
        _exporters = exporters.ToList();
    }

    public IReadOnlyList<string> SupportedFormats => _exporters.Select(e => e.Format).ToList();

    public FinalGradeDto GetFor(int studentId)
    {
        var student = _students.Find(new StudentId(studentId))
            ?? throw new NotFoundException($"Студент #{studentId} не найден.");
        return Calculate(student, _rubrics.Get(), _policy.Build());
    }

    public IReadOnlyList<FinalGradeDto> GetAll()
    {
        var rubric = _rubrics.Get();
        var strategy = _policy.Build();
        return _students.GetAll()
            .OrderBy(s => s.Id.Value)
            .Select(s => Calculate(s, rubric, strategy))
            .ToList();
    }

    public int Export(string format, string path)
    {
        var exporter = _exporters.FirstOrDefault(e => e.Format.Equals(format, StringComparison.OrdinalIgnoreCase))
            ?? throw new UseCaseException(
                $"Неизвестный формат «{format}». Доступно: {string.Join(", ", SupportedFormats)}.");

        var rows = GetAll();
        exporter.Export(rows, path);
        return rows.Count;
    }

    private FinalGradeDto Calculate(Student student, Rubric rubric, Domain.Grading.IGradingStrategy strategy)
    {
        var grade = _calculator.Calculate(student.Id, rubric, _evaluations.GetByStudent(student.Id), strategy);
        return new FinalGradeDto(
            student.Id.Value,
            student.FullName,
            grade.Value,
            grade.GradedCriteria,
            grade.TotalCriteria,
            strategy.Name);
    }
}
