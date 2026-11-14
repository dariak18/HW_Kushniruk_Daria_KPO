using System.Globalization;
using System.Text.Json;
using Pos.Application;
using Pos.Domain.Common;

namespace Pos.ConsoleApp;

/// <summary>Ошибка в самой команде (неверные аргументы), а не в предметной области</summary>
public sealed class CommandException : Exception
{
    public CommandException(string message) : base(message)
    {
    }
}

/// <summary>Консольный адаптер: разбирает строку, вызывает фасад, печатает результат</summary>
public sealed class CommandProcessor
{
    private readonly PosFacade _pos;
    private readonly GradingPolicy _policy;
    private readonly TextWriter _out;

    public CommandProcessor(PosFacade pos, GradingPolicy policy, TextWriter output)
    {
        _pos = pos;
        _policy = policy;
        _out = output;
    }

    /// <summary>Выполняет одну команду. Возвращает false, если нужно завершить работу</summary>
    public bool Execute(string line)
    {
        var text = line.Trim();
        if (text.Length == 0)
        {
            return true;
        }

        try
        {
            return Dispatch(text);
        }
        catch (Exception ex) when (ex is DomainException or UseCaseException or CommandException
            or IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            _out.WriteLine($"Ошибка: {ex.Message}");
            return true;
        }
    }

    private static string Fmt(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static int ParseInt(string raw, string what) =>
        int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new CommandException($"{what}: ожидалось целое число, получено «{raw}».");

    private static decimal ParseDecimal(string raw, string what) =>
        decimal.TryParse(raw.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new CommandException($"{what}: ожидалось число, получено «{raw}».");

    private bool Dispatch(string text)
    {
        var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        switch (tokens[0].ToLowerInvariant())
        {
            case "help":
                PrintHelp();
                return true;
            case "exit":
            case "quit":
                return false;
            case "student":
                HandleStudent(text);
                return true;
            case "criterion":
                HandleCriterion(text);
                return true;
            case "grade":
                HandleGrade(text);
                return true;
            case "result":
                HandleResult(tokens);
                return true;
            case "policy":
                HandlePolicy(tokens);
                return true;
            case "export":
                HandleExport(tokens);
                return true;
            case "demo":
                try
                {
                    new DemoScenario(_pos, this, _out).Run();
                }
                catch (Exception ex) when (ex is DomainException or UseCaseException)
                {
                    _out.WriteLine($"Ошибка в демо: {ex.Message}");
                }

                return true;
            default:
                throw new CommandException($"Неизвестная команда «{tokens[0]}». Введите help.");
        }
    }

    private void HandleStudent(string text)
    {
        var parts = text.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        switch (parts.ElementAtOrDefault(1)?.ToLowerInvariant())
        {
            case "list":
                var students = _pos.ListStudents();
                if (students.Count == 0)
                {
                    _out.WriteLine("Студентов пока нет.");
                }

                foreach (var s in students)
                {
                    _out.WriteLine($"  #{s.Id}  {s.FullName}");
                }

                break;
            case "add":
                var added = _pos.AddStudent(RequireArg(parts, 2, "student add <ФИО>"));
                _out.WriteLine($"Добавлен студент #{added.Id}: {added.FullName}");
                break;
            case "remove":
                _pos.RemoveStudent(ParseInt(RequireArg(parts, 2, "student remove <id>"), "id студента"));
                _out.WriteLine("Студент удалён вместе с его оценками.");
                break;
            default:
                throw new CommandException("Используйте: student list | add <ФИО> | remove <id>");
        }
    }

    private void HandleCriterion(string text)
    {
        var parts = text.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        switch (parts.ElementAtOrDefault(1)?.ToLowerInvariant())
        {
            case "list":
                var criteria = _pos.ListCriteria();
                if (criteria.Count == 0)
                {
                    _out.WriteLine("Рубрикатор пуст.");
                }

                foreach (var c in criteria)
                {
                    _out.WriteLine($"  #{c.Id}  {c.Name}  (макс. {Fmt(c.MaxScore)}, вес {Fmt(c.Weight)})");
                }

                break;
            case "add":
                var fields = RequireArg(parts, 2, "сriterion add <название>;<макс. балл>;<вес>").Split(';');
                if (fields.Length != 3)
                {
                    throw new CommandException("Используйте: criterion add <название>;<макс. балл>;<вес>");
                }

                var added = _pos.AddCriterion(
                    fields[0],
                    ParseDecimal(fields[1].Trim(), "макс. балл"),
                    ParseDecimal(fields[2].Trim(), "вес"));
                _out.WriteLine($"Добавлен критерий #{added.Id}: {added.Name} (макс. {Fmt(added.MaxScore)}, вес {Fmt(added.Weight)})");
                break;
            case "remove":
                _pos.RemoveCriterion(ParseInt(RequireArg(parts, 2, "criterion remove <id>"), "id критерия"));
                _out.WriteLine("Критерий удалён вместе с оценками по нему.");
                break;
            default:
                throw new CommandException("Используйте: criterion list | add <название>;<макс>;<вес> | remove <id>");
        }
    }

    private void HandleGrade(string text)
    {
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        switch (parts.ElementAtOrDefault(1)?.ToLowerInvariant())
        {
            case "set":
                if (parts.Length < 5)
                {
                    throw new CommandException("Используйте: grade set <id студента> <id критерия> <балл> [комментарий]");
                }

                var comment = parts.Length > 5 ? string.Join(' ', parts.Skip(5)) : null;
                var result = _pos.SetGrade(
                    ParseInt(parts[2], "id студента"),
                    ParseInt(parts[3], "id критерия"),
                    ParseDecimal(parts[4], "балл"),
                    comment);
                var e = result.Evaluation;
                _out.WriteLine(
                    $"{(result.WasCorrection ? "Оценка исправлена" : "Оценка выставлена")}: " +
                    $"{e.CriterionName} — {Fmt(e.Score)} из {Fmt(e.MaxScore)}");
                break;
            case "list":
                var grades = _pos.ListGrades(ParseInt(RequireArg(parts, 2, "grade list <id студента>"), "id студента"));
                if (grades.Count == 0)
                {
                    _out.WriteLine("Оценок пока нет.");
                }

                foreach (var g in grades)
                {
                    var note = g.Comment is null ? string.Empty : $"  «{g.Comment}»";
                    _out.WriteLine(
                        $"  #{g.CriterionId}  {g.CriterionName}: {Fmt(g.Score)} / {Fmt(g.MaxScore)}  " +
                        $"({g.EvaluatedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm}){note}");
                }

                break;
            case "cancel":
                if (parts.Length < 4)
                {
                    throw new CommandException("Используйте: grade cancel <id студента> <id критерия>");
                }

                _pos.CancelGrade(ParseInt(parts[2], "id студента"), ParseInt(parts[3], "id критерия"));
                _out.WriteLine("Оценка отменена.");
                break;
            default:
                throw new CommandException("Используйте: grade set | list | cancel");
        }
    }

    private void HandleResult(string[] tokens)
    {
        var arg = RequireArg(tokens, 1, "result <id студента> | result all");
        if (arg.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var all = _pos.GetAllResults();
            if (all.Count == 0)
            {
                _out.WriteLine("Студентов пока нет.");
                return;
            }

            _out.WriteLine($"Правило расчёта: {all[0].Rule}");
            foreach (var r in all)
            {
                PrintResult(r);
            }

            return;
        }

        var single = _pos.GetResult(ParseInt(arg, "id студента"));
        _out.WriteLine($"Правило расчёта: {single.Rule}");
        PrintResult(single);
    }

    private void PrintResult(FinalGradeDto r) =>
        _out.WriteLine(
            $"  #{r.StudentId}  {r.StudentName,-20} итог {Fmt(r.Value),5} / 10   (оценено {r.GradedCriteria} из {r.TotalCriteria})");

    private void HandlePolicy(string[] tokens)
    {
        switch (tokens.ElementAtOrDefault(1)?.ToLowerInvariant())
        {
            case null:
                break;
            case "mode":
                _policy.Mode = RequireArg(tokens, 2, "policy mode strict|lenient").ToLowerInvariant() switch
                {
                    "strict" => GradingMode.Strict,
                    "lenient" => GradingMode.Lenient,
                    _ => throw new CommandException("Режим: strict или lenient"),
                };
                break;
            case "round":
                _policy.RoundResult = RequireArg(tokens, 2, "policy round on|off").ToLowerInvariant() switch
                {
                    "on" => true,
                    "off" => false,
                    _ => throw new CommandException("Используйте: policy round on|off"),
                };
                break;
            default:
                throw new CommandException("Используйте: policy [mode strict|lenient] [round on|off]");
        }

        _out.WriteLine($"Текущее правило: {_policy.Build().Name}");
    }

    private void HandleExport(string[] tokens)
    {
        if (tokens.Length < 3)
        {
            throw new CommandException(
                $"Используйте: export <{string.Join('|', _pos.SupportedExportFormats)}> <путь к файлу>");
        }

        var count = _pos.Export(tokens[1], string.Join(' ', tokens.Skip(2)));
        _out.WriteLine($"Выгружено строк: {count} → {string.Join(' ', tokens.Skip(2))}");
    }

    private static string RequireArg(string[] parts, int index, string usage) =>
        parts.Length > index ? parts[index] : throw new CommandException($"Используйте: {usage}");

    private void PrintHelp()
    {
        _out.WriteLine("Команды:");
        _out.WriteLine("  student list | add <ФИО> | remove <id>");
        _out.WriteLine("  criterion list | add <название>;<макс. балл>;<вес> | remove <id>");
        _out.WriteLine("  grade set <id студента> <id критерия> <балл> [комментарий]");
        _out.WriteLine("  grade list <id студента> | cancel <id студента> <id критерия>");
        _out.WriteLine("  result <id студента> | result all");
        _out.WriteLine("  policy [mode strict|lenient] [round on|off]");
        _out.WriteLine($"  export <{string.Join('|', _pos.SupportedExportFormats)}> <путь к файлу>");
        _out.WriteLine("  demo — сквозной сценарий (нужны пустые данные)");
        _out.WriteLine("  help | exit");
    }
}
