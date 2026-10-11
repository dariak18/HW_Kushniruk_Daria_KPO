using VyshKat.Domain;

namespace VyshKat.Services;

public sealed class Fleet
{
    private readonly List<Transport> _transport = [];
    private readonly List<IInventoryItem> _items = [];
    private readonly HashSet<string> _inventoryNumbers = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<Transport> Transport => _transport.AsReadOnly();
    public IReadOnlyList<IInventoryItem> Items => _items.AsReadOnly();

    public void AddTransport(Transport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);
        RegisterInventoryNumber(transport.InventoryNumber);
        _transport.Add(transport);
    }

    public void AddItem(IInventoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        RegisterInventoryNumber(item.InventoryNumber);
        _items.Add(item);
    }

    public decimal TotalDailyEnergyKwh() =>
        _transport.OfType<IEnergyConsumer>().Sum(x => x.DailyEnergyKwh)
        + _items.OfType<IEnergyConsumer>().Sum(x => x.DailyEnergyKwh);

    public IReadOnlyList<IRentableTransport> BeginnerFriendlyTransport() =>
        _transport.Where(x => x.BeginnerFriendliness >= 6).Cast<IRentableTransport>().ToList();

    public IReadOnlyList<IInventoryItem> AllInventoryItems() =>
        _transport.Cast<IInventoryItem>().Concat(_items).ToList();

    private void RegisterInventoryNumber(string inventoryNumber)
    {
        if (!_inventoryNumbers.Add(inventoryNumber))
            throw new InvalidOperationException($"Инвентарный номер «{inventoryNumber}» уже используется.");
    }
}
