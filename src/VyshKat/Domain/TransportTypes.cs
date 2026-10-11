namespace VyshKat.Domain;

public sealed class Scooter : PoweredTransport
{
    public Scooter(
        string inventoryNumber,
        int beginnerFriendliness,
        decimal dailyEnergyKwh = 1.2m,
        bool isTechnicallySound = true)
        : base(
            inventoryNumber,
            "Электросамокат",
            beginnerFriendliness,
            dailyEnergyKwh,
            isTechnicallySound)
    { }
}

public sealed class Bicycle : Transport
{
    public Bicycle(
        string inventoryNumber,
        int beginnerFriendliness,
        bool isTechnicallySound = true)
        : base(
            inventoryNumber,
            "Велосипед",
            beginnerFriendliness,
            isTechnicallySound)
    { }
}

public sealed class ElectricBicycle : PoweredTransport
{
    public ElectricBicycle(
        string inventoryNumber,
        int beginnerFriendliness,
        decimal dailyEnergyKwh = 0.8m,
        bool isTechnicallySound = true)
        : base(
            inventoryNumber,
            "Электровелосипед",
            beginnerFriendliness,
            dailyEnergyKwh,
            isTechnicallySound)
    { }
}
