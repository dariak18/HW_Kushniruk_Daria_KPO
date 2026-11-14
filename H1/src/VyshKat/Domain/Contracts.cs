namespace VyshKat.Domain;

public interface IInventoryItem
{
    string InventoryNumber { get; }
    string Name { get; }
}

public interface IEnergyConsumer
{
    decimal DailyEnergyKwh { get; }
}

public interface IRentableTransport : IInventoryItem
{
    int BeginnerFriendliness { get; }
}

public abstract class InventoryItem : IInventoryItem
{
    protected InventoryItem(string inventoryNumber, string name)
    {
        if (string.IsNullOrWhiteSpace(inventoryNumber))
            throw new ArgumentException("Инвентарный номер не может быть пустым.", nameof(inventoryNumber));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Название не может быть пустым.", nameof(name));
        InventoryNumber = inventoryNumber.Trim();
        Name = name.Trim();
    }

    public string InventoryNumber { get; }
    public string Name { get; }
    public override string ToString() => $"{Name} [инв. № {InventoryNumber}]";
}

public abstract class Transport : InventoryItem, IRentableTransport
{
    protected Transport(
        string inventoryNumber,
        string name,
        int beginnerFriendliness,
        bool isTechnicallySound = true)
        : base(inventoryNumber, name)
    {
        if (beginnerFriendliness is < 1 or > 10)
            throw new ArgumentOutOfRangeException(
                nameof(beginnerFriendliness),
                beginnerFriendliness,
                "Ошибка: простота для новичка должна быть целым числом от 1 до 10 включительно.");

        BeginnerFriendliness = beginnerFriendliness;
        IsTechnicallySound = isTechnicallySound;
    }

    public int BeginnerFriendliness { get; }
    public bool IsTechnicallySound { get; }
}

public abstract class PoweredTransport : Transport, IEnergyConsumer
{
    protected PoweredTransport(
        string inventoryNumber,
        string name,
        int beginnerFriendliness,
        decimal dailyEnergyKwh,
        bool isTechnicallySound = true)
        : base(inventoryNumber, name, beginnerFriendliness, isTechnicallySound)
    {
        if (dailyEnergyKwh < 0)
            throw new ArgumentOutOfRangeException(
                nameof(dailyEnergyKwh),
                dailyEnergyKwh,
                "Ошибка: расход энергии не может быть отрицательным.");

        DailyEnergyKwh = dailyEnergyKwh;
    }

    public decimal DailyEnergyKwh { get; }
}
