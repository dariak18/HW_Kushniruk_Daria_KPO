using Pos.Domain.Model;
using Pos.Domain.Repositories;
using Pos.Domain.ValueObjects;

namespace Pos.Application.UseCases;

public sealed class StudentUseCases
{
    private readonly IStudentRepository _students;
    private readonly IEvaluationRepository _evaluations;

    public StudentUseCases(IStudentRepository students, IEvaluationRepository evaluations)
    {
        _students = students;
        _evaluations = evaluations;
    }

    public StudentDto Add(string fullName)
    {
        var student = new Student(_students.NextId(), fullName);
        _students.Add(student);
        return ToDto(student);
    }

    public IReadOnlyList<StudentDto> List() =>
        _students.GetAll().OrderBy(s => s.Id.Value).Select(ToDto).ToList();

    public void Remove(int id)
    {
        var studentId = new StudentId(id);
        if (_students.Find(studentId) is null)
        {
            throw new NotFoundException($"Студент #{id} не найден.");
        }

        _students.Remove(studentId);
        _evaluations.RemoveByStudent(studentId);
    }

    private static StudentDto ToDto(Student s) => new(s.Id.Value, s.FullName);
}
