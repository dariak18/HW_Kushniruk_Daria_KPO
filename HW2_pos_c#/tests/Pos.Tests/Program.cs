using Pos.Tests;

var tests = DomainTests.All().Concat(ApplicationTests.All()).ToList();
var failed = 0;

foreach (var (name, body) in tests)
{
    try
    {
        body();
        Console.WriteLine($"  [OK]   {name}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.WriteLine($"  [FAIL] {name}\n         {ex.GetType().Name}: {ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine($"Тестов: {tests.Count}, успешно: {tests.Count - failed}, упало: {failed}");
return failed == 0 ? 0 : 1;
