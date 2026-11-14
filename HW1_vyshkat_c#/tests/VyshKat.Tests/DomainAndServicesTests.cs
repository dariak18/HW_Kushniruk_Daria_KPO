using Microsoft.Extensions.DependencyInjection;
using VyshKat;
using VyshKat.Domain;
using VyshKat.Services;
using Xunit;

namespace VyshKat.Tests;

public sealed class DomainAndServicesTests
{
    [Fact]
    public void Transport_rejects_beginner_score_outside_range()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Scooter("S-1", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Scooter("S-2", 11));
    }

    [Fact]
    public void Real_service_center_accepts_technically_sound_transport()
    {
        var service = new ServiceCenter();
        Assert.True(service.Inspect(new Scooter("SC-200", 8)));
    }

    [Fact]
    public void Real_service_center_rejects_faulty_transport()
    {
        var service = new ServiceCenter();
        Assert.False(service.Inspect(new Scooter("SC-201", 8, isTechnicallySound: false)));
    }

    [Fact]
    public void Console_explains_invalid_beginner_score_and_reprompts()
    {
        var fleet = new Fleet();
        var park = new ParkService(fleet, new AlwaysAcceptServiceCenter());
        var app = new ConsoleApp(park, new ReportService(fleet));
        using var writer = new StringWriter();
        app.Run(new StringReader("1\nда\nSC-202\n15\nabc\n7\n0\n"), writer);
        var text = writer.ToString();
        Assert.Contains("значение должно быть от 1 до 10 включительно", text);
        Assert.Contains("не является целым числом", text);
        Assert.Contains("SC-202", text);
        Assert.Contains(fleet.Transport, t => t.InventoryNumber == "SC-202");
    }

    [Fact]
    public void Energy_consumer_rejects_negative_energy()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Scooter("S-1", 7, -0.1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChargingStation("C-1", -1m));
    }

    [Fact]
    public void Inspection_success_adds_transport()
    {
        var fleet = new Fleet();
        var service = new ParkService(fleet, new AlwaysAcceptServiceCenter());
        Assert.True(service.AcceptTransport(new Scooter("S-1", 7, 1.5m)));
        Assert.Single(fleet.Transport);
    }

    [Fact]
    public void Inspection_failure_does_not_add_transport()
    {
        var fleet = new Fleet();
        var service = new ParkService(fleet, new AlwaysRejectServiceCenter());
        Assert.False(service.AcceptTransport(new Scooter("S-1", 7)));
        Assert.Empty(fleet.Transport);
    }

    [Fact]
    public void Duplicate_inventory_number_is_rejected_case_insensitively()
    {
        var fleet = new Fleet();
        fleet.AddTransport(new Scooter("SC-1", 8));
        Assert.Throws<InvalidOperationException>(() => fleet.AddItem(new Helmet("sc-1")));
    }

    [Fact]
    public void Beginner_report_uses_threshold_six_inclusively()
    {
        var fleet = new Fleet();
        fleet.AddTransport(new Scooter("S-6", 6));
        fleet.AddTransport(new Scooter("S-5", 5));
        var report = new ReportService(fleet);
        var beginnerList = report.BeginnerFriendly;
        Assert.Single(beginnerList);
        Assert.Equal("S-6", beginnerList[0].InventoryNumber);
    }

    [Fact]
    public void Energy_report_sums_transport_and_inventory_consumers()
    {
        var fleet = new Fleet();
        fleet.AddTransport(new Scooter("S-1", 8, 1.25m));
        fleet.AddTransport(new Bicycle("B-1", 9));
        fleet.AddItem(new ChargingStation("C-1", 2.5m));
        fleet.AddItem(new Helmet("H-1"));
        Assert.Equal(3.75m, new ReportService(fleet).TotalDailyEnergyKwh);
    }

    [Fact]
    public void Inventory_report_contains_transport_and_items()
    {
        var fleet = new Fleet();
        fleet.AddTransport(new Bicycle("B-1", 8));
        fleet.AddItem(new Helmet("H-1"));
        var report = new ReportService(fleet);
        Assert.Equal(2, report.InventoryItemCount);
        Assert.Equal(2, report.AllInventoryItems.Count);
    }

    [Fact]
    public void Di_container_resolves_services_and_inspection_abstraction()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Fleet>();
        services.AddSingleton<IServiceCenter, AlwaysAcceptServiceCenter>();
        services.AddSingleton<ParkService>();
        services.AddSingleton<ReportService>();
        services.AddSingleton<ConsoleApp>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        Assert.IsType<AlwaysAcceptServiceCenter>(provider.GetRequiredService<IServiceCenter>());
        Assert.NotNull(provider.GetRequiredService<ConsoleApp>());
    }

    [Fact]
    public void Console_demo_shows_reports_and_acceptance_scenarios()
    {
        var fleet = new Fleet();
        var park = new ParkService(fleet, new AlwaysAcceptServiceCenter());
        var app = new ConsoleApp(park, new ReportService(fleet));
        using var writer = new StringWriter();
        app.Run(new StringReader("7\n0\n"), writer);
        var text = writer.ToString();
        Assert.Contains("Суточное энергопотребление", text);
        Assert.Contains("Техника для новичков", text);
        Assert.Contains("DEMO-BI-999: отклонён сервисным центром", text);
        Assert.Contains("До свидания!", text);
    }

    [Fact]
    public void Console_demo_can_be_run_twice_without_crashing()
    {
        var fleet = new Fleet();
        var app = new ConsoleApp(
            new ParkService(fleet, new AlwaysAcceptServiceCenter()),
            new ReportService(fleet));
        using var writer = new StringWriter();

        app.Run(new StringReader("7\n7\n0\n"), writer);

        var text = writer.ToString();
        Assert.Contains("уже используется", text);
        Assert.Contains("До свидания!", text);
        Assert.Single(fleet.Transport, t => t.InventoryNumber == "DEMO-SC-104");
    }

    [Fact]
    public void Console_invalid_menu_choice_explains_error_and_exit_says_goodbye()
    {
        var fleet = new Fleet();
        var app = new ConsoleApp(
            new ParkService(fleet, new AlwaysAcceptServiceCenter()),
            new ReportService(fleet));
        using var writer = new StringWriter();

        app.Run(new StringReader("xyz\n0\n"), writer);

        var text = writer.ToString();
        Assert.Contains("Ошибка: выберите один из пунктов меню", text);
        Assert.Contains("Попробуйте ещё раз.", text);
        Assert.Contains("До свидания!", text);
    }

    [Fact]
    public void Console_adds_helmet_and_reprompts_for_empty_inventory_number()
    {
        var fleet = new Fleet();
        var app = new ConsoleApp(
            new ParkService(fleet, new AlwaysAcceptServiceCenter()),
            new ReportService(fleet));
        using var writer = new StringWriter();

        app.Run(new StringReader("4\nможет быть\nда\n   \nHELM-TEST\n0\n"), writer);

        var text = writer.ToString();
        Assert.Contains("Ошибка: введите «да» или «нет». Попробуйте ещё раз.", text);
        Assert.Contains("Ошибка: значение не может быть пустым. Попробуйте ещё раз.", text);
        Assert.Contains("Шлем № HELM-TEST добавлен в инвентарь.", text);
        Assert.Contains(fleet.Items, item => item.InventoryNumber == "HELM-TEST");
    }

    [Fact]
    public void Console_rejects_duplicate_helmet_inventory_number()
    {
        var fleet = new Fleet();
        var app = new ConsoleApp(
            new ParkService(fleet, new AlwaysAcceptServiceCenter()),
            new ReportService(fleet));
        using var writer = new StringWriter();

        app.Run(new StringReader("4\nда\nHE-001\n0\n"), writer);

        Assert.Contains("Ошибка добавления шлема", writer.ToString());
        Assert.Contains("Попробуйте ещё раз.", writer.ToString());
    }

    [Fact]
    public void Console_charging_station_rejects_invalid_and_negative_energy_then_accepts_valid_value()
    {
        var fleet = new Fleet();
        var app = new ConsoleApp(
            new ParkService(fleet, new AlwaysAcceptServiceCenter()),
            new ReportService(fleet));
        using var writer = new StringWriter();

        app.Run(new StringReader("5\nда\nCH-TEST\nabc\n-2\n2.4\n0\n"), writer);

        var text = writer.ToString();
        Assert.Contains("Ошибка: введите число, например: 2.4.", text);
        Assert.Contains("Ошибка: энергопотребление не может быть отрицательным.", text);
        Assert.Contains("Введите суточное энергопотребление в кВт·ч (например: 2.4):", text);
        Assert.Contains("Зарядная станция № CH-TEST добавлена.", text);
        Assert.Contains(fleet.Items, item => item.InventoryNumber == "CH-TEST");
    }

    [Fact]
    public void Console_cancelled_item_addition_does_not_change_inventory()
    {
        var fleet = new Fleet();
        var app = new ConsoleApp(
            new ParkService(fleet, new AlwaysAcceptServiceCenter()),
            new ReportService(fleet));
        using var writer = new StringWriter();

        app.Run(new StringReader("4\nнет\n0\n"), writer);

        Assert.Contains("Добавление шлема отменено.", writer.ToString());
        Assert.DoesNotContain(fleet.Items, item => item.InventoryNumber == "CANCELLED");
    }

    private sealed class AlwaysAcceptServiceCenter : IServiceCenter
    {
        public bool Inspect(Transport transport) => true;
    }

    private sealed class AlwaysRejectServiceCenter : IServiceCenter
    {
        public bool Inspect(Transport transport) => false;
    }
}
