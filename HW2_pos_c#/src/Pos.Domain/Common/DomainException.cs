namespace Pos.Domain.Common;

/// <summary>Нарушение инварианта предметной области</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
