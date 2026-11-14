using Pos.Domain.Model;
using Pos.Domain.ValueObjects;

namespace Pos.Domain.Repositories;

// Порты хранилища. Домен знает только эти интерфейсы; JSON, файлы и прочее живут снаружи

public interface IStudentRepository
{
    StudentId NextId();

    void Add(Student student);

    Student? Find(StudentId id);

    IReadOnlyList<Student> GetAll();

    void Remove(StudentId id);
}

/// <summary>В модуле один рубрикатор (один курс). Для нескольких курсов порт можно расширить</summary>
public interface IRubricRepository
{
    Rubric Get();

    void Save(Rubric rubric);
}

public interface IEvaluationRepository
{
    Evaluation? Find(StudentId studentId, CriterionId criterionId);

    IReadOnlyList<Evaluation> GetByStudent(StudentId studentId);

    IReadOnlyList<Evaluation> GetAll();

    /// <summary>Добавляет оценку или заменяет существующую с тем же идентификатором</summary>
    void Save(Evaluation evaluation);

    void Remove(StudentId studentId, CriterionId criterionId);

    void RemoveByStudent(StudentId studentId);

    void RemoveByCriterion(CriterionId criterionId);
}
