using Microsoft.Extensions.DependencyInjection;
using Pos.Application;
using Pos.Application.Ports;
using Pos.Application.UseCases;
using Pos.Domain.Factories;
using Pos.Domain.Repositories;
using Pos.Domain.Services;
using Pos.Infrastructure.Export;
using Pos.Infrastructure.Persistence;

namespace Pos.ConsoleApp;

/// <summary>
/// Composition root: единственное место, где известны все слои сразу.
/// Здесь абстракции (порты) связываются с конкретными адаптерами
/// </summary>
public static class CompositionRoot
{
    public static ServiceProvider Build(string dataDirectory, TextWriter output)
    {
        var services = new ServiceCollection();

        // Инфраструктура: адаптеры портов домена и приложения
        services.AddSingleton(new StorageOptions(dataDirectory));
        services.AddSingleton<IStudentRepository, JsonStudentRepository>();
        services.AddSingleton<IRubricRepository, JsonRubricRepository>();
        services.AddSingleton<IEvaluationRepository, JsonEvaluationRepository>();
        services.AddSingleton<IResultExporter, CsvResultExporter>();
        services.AddSingleton<IResultExporter, JsonResultExporter>();
        services.AddSingleton(TimeProvider.System);

        // Домен: фабрика и доменный сервис без состояния
        services.AddSingleton<EvaluationFactory>();
        services.AddSingleton<FinalGradeCalculator>();

        // Приложение: настройка расчёта, сценарии и фасад
        services.AddSingleton<GradingPolicy>();
        services.AddSingleton<StudentUseCases>();
        services.AddSingleton<RubricUseCases>();
        services.AddSingleton<EvaluationUseCases>();
        services.AddSingleton<ResultsUseCases>();
        services.AddSingleton<PosFacade>();

        // UI
        services.AddSingleton(output);
        services.AddSingleton<CommandProcessor>();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }
}
