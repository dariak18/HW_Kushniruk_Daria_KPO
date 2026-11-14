using Pos.Domain.Model;
using Pos.Domain.Repositories;
using Pos.Domain.ValueObjects;

namespace Pos.Infrastructure.Persistence;

/// <summary>Адаптер порта <see cref="IStudentRepository"/>: хранит студентов в students.json</summary>
public sealed class JsonStudentRepository : IStudentRepository
{
    private sealed record Record(int Id, string FullName);

    private readonly string _path;
    private readonly Dictionary<StudentId, Student> _items;

    public JsonStudentRepository(StorageOptions options)
    {
        _path = Path.Combine(options.DataDirectory, "students.json");
        _items = JsonFile.Load<Record>(_path)
            .Select(r => new Student(new StudentId(r.Id), r.FullName))
            .ToDictionary(s => s.Id);
    }

    public StudentId NextId() => new(_items.Count == 0 ? 1 : _items.Keys.Max(k => k.Value) + 1);

    public void Add(Student student)
    {
        _items[student.Id] = student;
        Persist();
    }

    public Student? Find(StudentId id) => _items.GetValueOrDefault(id);

    public IReadOnlyList<Student> GetAll() => _items.Values.ToList();

    public void Remove(StudentId id)
    {
        if (_items.Remove(id))
        {
            Persist();
        }
    }

    private void Persist() =>
        JsonFile.Save(_path, _items.Values.OrderBy(s => s.Id.Value).Select(s => new Record(s.Id.Value, s.FullName)));
}
