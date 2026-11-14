using VyshKat.Domain;
using VyshKat.Services;

namespace VyshKat;

public sealed class ConsoleApp(ParkService park, ReportService reports)
{
    private readonly ParkService _park = park;
    private readonly ReportService _reports = reports;

    public void Run(TextReader input, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        output.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        output.WriteLine("       ВышКат — Доедем до дедлайна");
        output.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        SeedDemoFleet(output);

        while (true)
        {
            PrintMenu(output);
            output.Write("Выберите пункт меню: ");
            var choice = input.ReadLine();

            if (choice is null)
            {
                output.WriteLine("Ввод завершён. До свидания!");
                return;
            }

            switch (choice.Trim())
            {
                case "1":
                    TryAddTransport(input, output, 1);
                    break;
                case "2":
                    TryAddTransport(input, output, 2);
                    break;
                case "3":
                    TryAddTransport(input, output, 3);
                    break;
                case "4":
                    TryAddHelmet(input, output);
                    break;
                case "5":
                    TryAddChargingStation(input, output);
                    break;
                case "6":
                    PrintReports(output);
                    break;
                case "7":
                    RunDemoScenario(output);
                    break;
                case "0":
                    output.WriteLine("До свидания!");
                    return;
                default:
                    output.WriteLine("Ошибка: выберите один из пунктов меню — 1, 2, 3, 4, 5, 6, 7 или 0. Попробуйте ещё раз.");
                    break;
            }

            output.WriteLine();
        }
    }

    private static void PrintMenu(TextWriter output)
    {
        output.WriteLine("Меню:");
        output.WriteLine("1. Добавить электросамокат");
        output.WriteLine("2. Добавить велосипед");
        output.WriteLine("3. Добавить электровелосипед");
        output.WriteLine("4. Добавить шлем");
        output.WriteLine("5. Добавить зарядную станцию");
        output.WriteLine("6. Показать отчёты");
        output.WriteLine("7. Запустить демонстрационный сценарий");
        output.WriteLine("0. Выход");
    }

    private void TryAddTransport(TextReader input, TextWriter output, int type)
    {
        if (!AskYesNo(input, output, "Хотите добавить транспорт? (да/нет): "))
        {
            output.WriteLine("Добавление транспорта отменено.");
            return;
        }

        var inventoryNumber = ReadRequiredText(input, output, "Введите инвентарный номер транспорта: ");
        if (inventoryNumber is null)
        {
            output.WriteLine("Транспорт не добавлен.");
            return;
        }

        var score = ReadBeginnerFriendliness(input, output);
        if (score is null)
        {
            output.WriteLine("Транспорт не добавлен.");
            return;
        }

        try
        {
            Transport transport = type switch
            {
                1 => new Scooter(inventoryNumber, score.Value),
                2 => new Bicycle(inventoryNumber, score.Value),
                3 => new ElectricBicycle(inventoryNumber, score.Value),
                _ => throw new InvalidOperationException("Неизвестный тип транспорта.")
            };

            if (_park.AcceptTransport(transport))
                output.WriteLine($"Готово: {transport.Name} № {transport.InventoryNumber} прошёл техосмотр и добавлен в парк.");
            else
                output.WriteLine($"Отказ: {transport.Name} № {transport.InventoryNumber} не прошёл технический осмотр.");
        }
        catch (InvalidOperationException ex)
        {
            output.WriteLine($"Ошибка добавления: {ex.Message} Попробуйте ещё раз.");
        }
    }

    private void TryAddHelmet(TextReader input, TextWriter output)
    {
        if (!AskYesNo(input, output, "Хотите добавить шлем? (да/нет): "))
        {
            output.WriteLine("Добавление шлема отменено.");
            return;
        }

        var inventoryNumber = ReadRequiredText(input, output, "Введите инвентарный номер шлема: ");
        if (inventoryNumber is null)
        {
            output.WriteLine("Шлем не добавлен.");
            return;
        }

        try
        {
            _park.RegisterItem(new Helmet(inventoryNumber));
            output.WriteLine($"Шлем № {inventoryNumber} добавлен в инвентарь.");
        }
        catch (InvalidOperationException ex)
        {
            output.WriteLine($"Ошибка добавления шлема: {ex.Message} Попробуйте ещё раз.");
        }
    }

    private void TryAddChargingStation(TextReader input, TextWriter output)
    {
        if (!AskYesNo(input, output, "Хотите добавить зарядную станцию? (да/нет): "))
        {
            output.WriteLine("Добавление зарядной станции отменено.");
            return;
        }

        var inventoryNumber = ReadRequiredText(input, output, "Введите инвентарный номер зарядной станции: ");
        if (inventoryNumber is null)
        {
            output.WriteLine("Зарядная станция не добавлена.");
            return;
        }

        var energy = ReadNonNegativeDecimal(input, output, "Введите суточное энергопотребление в кВт·ч (например: 2.4): ");
        if (energy is null)
        {
            output.WriteLine("Зарядная станция не добавлена.");
            return;
        }

        try
        {
            _park.RegisterItem(new ChargingStation(inventoryNumber, energy.Value));
            output.WriteLine($"Зарядная станция № {inventoryNumber} добавлена. Суточное энергопотребление: {energy.Value:F2} кВт·ч.");
        }
        catch (InvalidOperationException ex)
        {
            output.WriteLine($"Ошибка добавления зарядной станции: {ex.Message} Попробуйте ещё раз.");
        }
    }

    private static bool AskYesNo(TextReader input, TextWriter output, string prompt)
    {
        while (true)
        {
            output.Write(prompt);
            var answer = input.ReadLine();
            if (answer is null)
            {
                output.WriteLine("Ошибка: ввод завершён. Попробуйте ещё раз.");
                return false;
            }

            var normalized = answer.Trim();
            if (normalized.Equals("да", StringComparison.OrdinalIgnoreCase))
                return true;
            if (normalized.Equals("нет", StringComparison.OrdinalIgnoreCase))
                return false;

            output.WriteLine("Ошибка: введите «да» или «нет». Попробуйте ещё раз.");
        }
    }

    private static string? ReadRequiredText(TextReader input, TextWriter output, string prompt)
    {
        while (true)
        {
            output.Write(prompt);
            var value = input.ReadLine();
            if (value is null)
                return null;

            value = value.Trim();
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            output.WriteLine("Ошибка: значение не может быть пустым. Попробуйте ещё раз.");
        }
    }

    private static int? ReadIntegerInRange(TextReader input, TextWriter output, string prompt, int min, int max)
    {
        while (true)
        {
            output.Write(prompt);
            var value = input.ReadLine();
            if (value is null)
                return null;
            if (!int.TryParse(value, out var number))
            {
                output.WriteLine($"Ошибка: «{value}» не является целым числом. Введите число от {min} до {max}. Попробуйте ещё раз.");
                continue;
            }
            if (number < min || number > max)
            {
                output.WriteLine($"Ошибка: значение должно быть от {min} до {max} включительно. Попробуйте ещё раз.");
                continue;
            }
            return number;
        }
    }

    private static int? ReadBeginnerFriendliness(TextReader input, TextWriter output) =>
        ReadIntegerInRange(input, output, "Введите простоту для новичка (целое число от 1 до 10): ", 1, 10);

    private static decimal? ReadNonNegativeDecimal(TextReader input, TextWriter output, string prompt)
    {
        while (true)
        {
            output.Write(prompt);
            var value = input.ReadLine();
            if (value is null)
                return null;

            if (!decimal.TryParse(value, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.CurrentCulture, out var number)
                && !decimal.TryParse(value, System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out number))
            {
                output.WriteLine("Ошибка: введите число, например: 2.4. Отрицательные значения недопустимы. Попробуйте ещё раз.");
                continue;
            }

            if (number < 0)
            {
                output.WriteLine("Ошибка: энергопотребление не может быть отрицательным. Попробуйте ещё раз.");
                continue;
            }

            return number;
        }
    }

    private void RunDemoScenario(TextWriter output)
    {
        output.WriteLine("Демонстрационный сценарий:");
        try
        {
            var accepted = _park.AcceptTransport(new Scooter("DEMO-SC-104", 8, 1.1m));
            output.WriteLine(accepted
                ? "DEMO-SC-104: техосмотр пройден, транспорт добавлен."
                : "DEMO-SC-104: транспорт отклонён сервисным центром.");
        }
        catch (InvalidOperationException ex)
        {
            output.WriteLine($"DEMO-SC-104: не добавлен — {ex.Message}");
        }

        var rejectedPark = new ParkService(new Fleet(), new RejectingServiceCenter());
        var rejectedResult = rejectedPark.AcceptTransport(new Bicycle("DEMO-BI-999", 7));
        output.WriteLine(rejectedResult
            ? "DEMO-BI-999: принят."
            : "DEMO-BI-999: отклонён сервисным центром (демонстрация отказа).");
        PrintReports(output);
    }

    private void SeedDemoFleet(TextWriter output)
    {
        try
        {
            if (_reports.InventoryItemCount == 0)
            {
                _park.RegisterItem(new Helmet("HE-001"));
                _park.RegisterItem(new ChargingStation("CH-001", 2.4m));
                _park.AcceptTransport(new Scooter("SC-101", 8, 1.2m));
                _park.AcceptTransport(new Bicycle("BI-101", 9));
                _park.AcceptTransport(new ElectricBicycle("EB-101", 5, 0.8m));
            }
        }
        catch (InvalidOperationException ex)
        {
            output.WriteLine($"Предупреждение при заполнении примера: {ex.Message}");
        }
    }

    private void PrintReports(TextWriter output)
    {
        output.WriteLine("Отчёты по парку:");
        output.WriteLine($"Транспортных средств в парке: {_reports.TransportCount}");
        output.WriteLine($"Всего инвентарных объектов: {_reports.InventoryItemCount}");
        output.WriteLine($"Суточное энергопотребление: {_reports.TotalDailyEnergyKwh:F2} кВт·ч");
        output.WriteLine("Техника для новичков (простота ≥ 6):");
        foreach (var transport in _reports.BeginnerFriendly)
            output.WriteLine($"  - {transport.Name}, № {transport.InventoryNumber}, простота {transport.BeginnerFriendliness}/10");
        output.WriteLine("Весь транспорт и инвентарь:");
        foreach (var item in _reports.AllInventoryItems)
            output.WriteLine($"  - {item}");
    }

    private sealed class RejectingServiceCenter : IServiceCenter
    {
        public bool Inspect(Transport transport) => false;
    }
}
