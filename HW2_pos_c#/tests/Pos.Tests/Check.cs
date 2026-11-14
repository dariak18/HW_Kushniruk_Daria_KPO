namespace Pos.Tests;

/// <summary>Минимальные проверки: внешние тестовые пакеты (xunit/NUnit) в этой среде недоступны</summary>
public static class Check
{
    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new TestFailedException($"Ожидалось {expected}, получено {actual}");
        }
    }

    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new TestFailedException(message);
        }
    }

    public static TException Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException ex)
        {
            return ex;
        }

        throw new TestFailedException($"Ожидалось исключение {typeof(TException).Name}, но его не было");
    }
}

public sealed class TestFailedException : Exception
{
    public TestFailedException(string message) : base(message)
    {
    }
}
