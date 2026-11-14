using VyshKat.Domain;

namespace VyshKat.Services;

public interface IServiceCenter
{
    bool Inspect(Transport transport);
}

public sealed class ServiceCenter : IServiceCenter
{
    public bool Inspect(Transport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);
        // Решение зависит от технического состояния, а не от оценки удобства для новичка.
        return transport.IsTechnicallySound;
    }
}
