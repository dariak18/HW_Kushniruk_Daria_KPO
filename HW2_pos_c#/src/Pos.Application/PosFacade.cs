using Pos.Application.UseCases;

namespace Pos.Application;

/// <summary>
/// Единая точка входа в сценарии модуля (GoF: Facade). Консоли не нужно знать, сколько
/// use case-классов за ней стоит и как они между собой связаны
/// </summary>
public sealed class PosFacade
{
    private readonly StudentUseCases _students;
    private readonly RubricUseCases _rubric;
    private readonly EvaluationUseCases _evaluations;
    private readonly ResultsUseCases _results;

    public PosFacade(
        StudentUseCases students,
        RubricUseCases rubric,
        EvaluationUseCases evaluations,
        ResultsUseCases results)
    {
        _students = students;
        _rubric = rubric;
        _evaluations = evaluations;
        _results = results;
    }

    public IReadOnlyList<string> SupportedExportFormats => _results.SupportedFormats;

    public StudentDto AddStudent(string fullName) => _students.Add(fullName);

    public IReadOnlyList<StudentDto> ListStudents() => _students.List();

    public void RemoveStudent(int id) => _students.Remove(id);

    public CriterionDto AddCriterion(string name, decimal maxScore, decimal weight) =>
        _rubric.AddCriterion(name, maxScore, weight);

    public IReadOnlyList<CriterionDto> ListCriteria() => _rubric.List();

    public void RemoveCriterion(int id) => _rubric.Remove(id);

    public SetGradeResult SetGrade(int studentId, int criterionId, decimal score, string? comment = null) =>
        _evaluations.Set(studentId, criterionId, score, comment);

    public IReadOnlyList<EvaluationDto> ListGrades(int studentId) => _evaluations.List(studentId);

    public void CancelGrade(int studentId, int criterionId) => _evaluations.Cancel(studentId, criterionId);

    public FinalGradeDto GetResult(int studentId) => _results.GetFor(studentId);

    public IReadOnlyList<FinalGradeDto> GetAllResults() => _results.GetAll();

    public int Export(string format, string path) => _results.Export(format, path);
}
