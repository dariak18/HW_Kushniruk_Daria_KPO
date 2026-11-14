namespace Pos.Application;

/// <summary>Сценарий нельзя выполнить (не нарушение домена, а ошибка запроса)</summary>
public class UseCaseException : Exception
{
    public UseCaseException(string message) : base(message)
    {
    }
}

public sealed class NotFoundException : UseCaseException
{
    public NotFoundException(string message) : base(message)
    {
    }
}
