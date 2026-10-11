using VyshKat.Domain;

namespace VyshKat.Services;

public sealed class ReportService(Fleet fleet)
{
    private readonly Fleet _fleet = fleet ?? throw new ArgumentNullException(nameof(fleet));

    public int TransportCount => _fleet.Transport.Count;
    public int InventoryItemCount => _fleet.AllInventoryItems().Count;
    public decimal TotalDailyEnergyKwh => _fleet.TotalDailyEnergyKwh();
    public IReadOnlyList<IRentableTransport> BeginnerFriendly => _fleet.BeginnerFriendlyTransport();
    public IReadOnlyList<IInventoryItem> AllInventoryItems => _fleet.AllInventoryItems();
}
