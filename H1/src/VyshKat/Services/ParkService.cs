using VyshKat.Domain;

namespace VyshKat.Services;

public sealed class ParkService(Fleet fleet, IServiceCenter serviceCenter)
{
    private readonly Fleet _fleet = fleet ?? throw new ArgumentNullException(nameof(fleet));
    private readonly IServiceCenter _serviceCenter = serviceCenter ?? throw new ArgumentNullException(nameof(serviceCenter));

    public bool AcceptTransport(Transport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);
        if (!_serviceCenter.Inspect(transport))
            return false;
        _fleet.AddTransport(transport);
        return true;
    }

    public void RegisterItem(IInventoryItem item) => _fleet.AddItem(item);
}
