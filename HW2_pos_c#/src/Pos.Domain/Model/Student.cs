using Pos.Domain.Common;
using Pos.Domain.ValueObjects;

namespace Pos.Domain.Model;

/// <summary>Студент — тот, кого оценивают. Корень собственного агрегата.</summary>
public sealed class Student : Entity<StudentId>
{
    public Student(StudentId id, string fullName) : base(id)
    {
        FullName = Normalize(fullName);
    }

    public string FullName { get; private set; }

    public void Rename(string fullName) => FullName = Normalize(fullName);

    private static string Normalize(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainException("ФИО студента не может быть пустым.");
        }

        return fullName.Trim();
    }
}
