using Pos.Application;

namespace Pos.ConsoleApp;

/// <summary>
/// Сквозной сценарий: студенты и критерии -> оценки -> нарушения инвариантов -> итог -> экспорт
/// Команды выполняются тем же процессором, что и ввод пользователя, и печатаются как в консоли
/// </summary>
public sealed class DemoScenario
{
    private const string ExportDirectory = "export";

    private readonly PosFacade _pos;
    private readonly CommandProcessor _processor;
    private readonly TextWriter _out;

    public DemoScenario(PosFacade pos, CommandProcessor processor, TextWriter output)
    {
        _pos = pos;
        _processor = processor;
        _out = output;
    }

    public void Run()
    {
        if (_pos.ListStudents().Count > 0 || _pos.ListCriteria().Count > 0)
        {
            _out.WriteLine("Для демо нужны пустые данные. Запустите приложение с --data <новая папка>.");
            return;
        }

        Step("1. Заводим студентов");
        Run("student add Кушнирук Дарья");
        Run("student add Пронина Ульяна");
        Run("student add Кузьмина Юлия");
        var students = _pos.ListStudents();

        Step("2. Собираем рубрикатор");
        Run("criterion add Домашние задания;10;3");
        Run("criterion add Контрольная;10;5");
        Run("criterion add Семинары;5;2");
        var criteria = _pos.ListCriteria();
        var (homework, test, seminar) = (criteria[0].Id, criteria[1].Id, criteria[2].Id);
        var (daria, ulyana, yulia) = (students[0].Id, students[1].Id, students[2].Id);

        Step("3. Выставляем оценки");
        Run($"grade set {daria} {homework} 10");
        Run($"grade set {daria} {test} 8 Хорошая работа");
        Run($"grade set {daria} {seminar} 5");
        Run($"grade set {ulyana} {homework} 10");
        Run($"grade set {ulyana} {test} 9");
        Run($"grade set {ulyana} {seminar} 4");
        Run($"grade set {yulia} {homework} 6");
        Run($"grade set {yulia} {test} 4");

        Step("4. Домен не даёт нарушить правила");
        Run($"grade set {daria} {test} 12");
        Run($"grade set {daria} 99 5");
        Run($"grade set {daria} {test} -1");

        Step("5. Исправляем и отменяем оценки");
        Run($"grade set {yulia} {test} 5 Пересмотрено после апелляции");
        Run($"grade list {yulia}");
        Run($"grade set {yulia} {seminar} 3");
        Run($"grade cancel {yulia} {seminar}");

        Step("6. Итоги: строгое правило (нет оценки = 0)");
        Run("result all");

        Step("7. Меняем правило расчёта на лету (Strategy + Decorator)");
        Run("policy mode lenient");
        Run("result all");

        Step("8. Экспорт итога");
        Run($"export csv {ExportDirectory}/results.csv");
        Run($"export json {ExportDirectory}/results.json");
        _out.WriteLine($"--- {ExportDirectory}/results.csv ---");
        _out.WriteLine(File.ReadAllText($"{ExportDirectory}/results.csv").TrimEnd());
    }

    private void Step(string title)
    {
        _out.WriteLine();
        _out.WriteLine($"~~~ {title} ~~~");
    }

    private void Run(string command)
    {
        _out.WriteLine($"pos> {command}");
        _processor.Execute(command);
    }
}
