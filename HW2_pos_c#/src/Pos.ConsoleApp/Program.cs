using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Pos.ConsoleApp;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

try
{
    var dataDirectory = OptionValue(args, "--data") ?? "data";
    var runDemo = args.Contains("--demo");
    
    using var provider = CompositionRoot.Build(dataDirectory, Console.Out);
    var processor = provider.GetRequiredService<CommandProcessor>();
    
    if (runDemo)
    {
        processor.Execute("demo");
        return;
    }
    
    Console.WriteLine("~ПОС — Преподавательская Оценка Студентов~\n");
    Console.WriteLine("Введите help для списка команд\n");
    Console.WriteLine($"Данные: {Path.GetFullPath(dataDirectory)}");
    while (true)
    {
        Console.Write("pos> ");
        var line = Console.ReadLine();
        if (line is null || !processor.Execute(line))
        {
            break;
        }
    }

}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
    or System.Text.Json.JsonException or Pos.Domain.Common.DomainException or ArgumentException)
{
    Console.Error.WriteLine($"Не удалось открыть данные или выполнить операцию: {ex.Message}");
    Environment.ExitCode = 1;
}

static string? OptionValue(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
