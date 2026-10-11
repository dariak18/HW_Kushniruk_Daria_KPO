namespace VyshKat.Domain;

public sealed class Helmet : InventoryItem
{
    public Helmet(string inventoryNumber, string name = "Шлем") : base(inventoryNumber, name) { }
}

public sealed class ChargingStation : InventoryItem, IEnergyConsumer
{
    public ChargingStation(string inventoryNumber, decimal dailyEnergyKwh, string name = "Зарядная станция")
        : base(inventoryNumber, name)
    {
        if (dailyEnergyKwh < 0)
            throw new ArgumentOutOfRangeException(nameof(dailyEnergyKwh), "Расход энергии не может быть отрицательным.");
        DailyEnergyKwh = dailyEnergyKwh;
    }

    public decimal DailyEnergyKwh { get; }
}
