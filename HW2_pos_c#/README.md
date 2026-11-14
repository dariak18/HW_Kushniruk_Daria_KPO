# ПОС — Преподавательская Оценка Студентов

Консольный модуль учёта оценок студентов преподавателем. 
(DDD, слоёная архитектура, паттерны GoF). Язык — C# / .NET 10.

## Запуск

```bash
# интерактивный режим (данные в ./data)
dotnet run --project src/Pos.ConsoleApp

# сквозной демо-сценарий на чистых данных
dotnet run --project src/Pos.ConsoleApp -- --demo --data ./demo-run

# тесты
dotnet run --project tests/Pos.Tests
```

Для демо нужна пустая папка `demo-run`. При повторном запуске укажите новое имя папки.
`demo-data` содержит готовый пример: его можно открыть через `--data ./demo-data` без `--demo`.

**Список команд**: `help`.

| Команда | Что делает |
|---|---|
| `student list` / `add <ФИО>` / `remove <id>` | студенты |
| `criterion list` / `add <название>;<макс>;<вес>` / `remove <id>` | рубрикатор |
| `grade set <студент> <критерий> <балл> [комментарий]` | выставить или исправить оценку |
| `grade list <студент>` / `grade cancel <студент> <критерий>` | посмотреть / отменить |
| `result <студент>` / `result all` | итог по студенту / по всем |
| `policy [mode strict\|lenient] [round on\|off]` | правило расчёта итога |
| `export csv\|json <путь>` | выгрузка итогов |
| `demo` | сквозной сценарий (нужны пустые данные) |

## Структура проекта

``` text
 HW2_Kushniruk_pos/
├── src/
│   ├── Pos.Domain/
│   │   ├── Common/
│   │   │   ├── DomainException.cs
│   │   │   └── Entity.cs
│   │   ├── Model/
│   │   │   ├── Student.cs
│   │   │   ├── Criterion.cs
│   │   │   ├── Rubric.cs
│   │   │   └── Evaluation.cs
│   │   ├── ValueObjects/
│   │   │   ├── Identifiers.cs
│   │   │   ├── Measures.cs
│   │   │   └── FinalGrade.cs
│   │   ├── Factories/
│   │   │   └── EvaluationFactory.cs
│   │   ├── Grading/
│   │   │   ├── IGradingStrategy.cs
│   │   │   ├── WeightedAverageStrategy.cs
│   │   │   ├── GradedOnlyWeightedAverageStrategy.cs
│   │   │   └── RoundingGradingStrategy.cs
│   │   ├── Services/
│   │   │   └── FinalGradeCalculator.cs
│   │   ├── Repositories/
│   │   │   └── Repositories.cs
│   │   └── Pos.Domain.csproj
│   ├── Pos.Application/
│   │   ├── Ports/
│   │   │   └── IResultExporter.cs
│   │   ├── UseCases/
│   │   │   ├── StudentUseCases.cs
│   │   │   ├── RubricUseCases.cs
│   │   │   ├── EvaluationUseCases.cs
│   │   │   └── ResultsUseCases.cs
│   │   ├── Dtos.cs
│   │   ├── Exceptions.cs
│   │   ├── GradingPolicy.cs
│   │   ├── PosFacade.cs
│   │   └── Pos.Application.csproj
│   ├── Pos.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── JsonFile.cs
│   │   │   ├── JsonStudentRepository.cs
│   │   │   ├── JsonRubricRepository.cs
│   │   │   └── JsonEvaluationRepository.cs
│   │   ├── Export/
│   │   │   ├── ResultExporterBase.cs
│   │   │   ├── CsvResultExporter.cs
│   │   │   └── JsonResultExporter.cs
│   │   └── Pos.Infrastructure.csproj
│   └── Pos.ConsoleApp/
│       ├── Program.cs
│       ├── CompositionRoot.cs
│       ├── CommandProcessor.cs
│       ├── DemoScenario.cs
│       └── Pos.ConsoleApp.csproj
├── tests/
│   └── Pos.Tests/
│       ├── Program.cs
│       ├── Check.cs
│       ├── DomainTests.cs
│       ├── ApplicationTests.cs
│       └── Pos.Tests.csproj
├── demo-data/
│   ├── students.json
│   ├── rubric.json
│   └── evaluations.json
├── export/
│   ├── results.csv
│   └── results.json
├── Pos.sln
├── global.json
└── README.md
```

## Логика решений

Ядро — доменная модель, а не таблица с оценками. Правила из ТЗ охраняет сама модель, а не UI:

- **балл не выходит за пределы критерия** — проверяет сущность `Evaluation` (при создании и при исправлении);
- **оценка только по критерию из рубрикатора** — рубрикатор (`Rubric`) единственный знает, какие критерии существуют; `Rubric.Get` бросает `DomainException` для неизвестного критерия;
- **итог согласован с набором оценок** — это не хранимое число, а значение, которое считает Domain Service `FinalGradeCalculator` из текущих оценок и рубрикатора. Поставить итог руками невозможно.

Дополнительные решения:

- Одна пара «студент + критерий» — одна оценка. Повторное выставление — это исправление (`Evaluation.Correct`), а не дубликат.
- Удаление критерия/студента каскадно удаляет его оценки на уровне сценария (use case), чтобы не оставалось «висячих» оценок.
- Шкала итога — 0..10. Если не оценили по всем критериям, поведение определяет выбранная стратегия (см. GoF).

## Карта DDD

**Ubiquitous Language** — в коде те же слова, что в ТЗ: `Student` (студент), `Criterion` (критерий), `Rubric` (рубрикатор), `Evaluation` (оценка), `Score`, `Weight`, `MaxScore`, `FinalGrade` (итог).

| Понятие | Тип DDD | Идентичность / инварианты |
|---|---|---|
| `Student` | Entity, корень агрегата | `StudentId`; ФИО не пустое |
| `Rubric` | Entity (корень агрегата) | владеет `Criterion`; названия уникальны; единственный источник «существующих» критериев |
| `Criterion` | Entity внутри агрегата `Rubric` | `CriterionId`; название не пустое, `MaxScore > 0`, `Weight > 0` |
| `Evaluation` | Entity, корень агрегата | идентичность `EvaluationId = (StudentId, CriterionId)`; `0 <= Score <= MaxScore критерия` |
| `Score`, `Weight`, `MaxScore` | Value Object | неизменяемы, проверяют себя в конструкторе, равны по значению |
| `StudentId`, `CriterionId`, `EvaluationId` | Value Object | положительные идентификаторы |
| `FinalGrade` | Value Object | значение в `[0, 10]`; создаётся только расчётом |
| `FinalGradeCalculator` | Domain Service | правило затрагивает рубрикатор и оценки сразу — не принадлежит одной сущности |
| `IStudentRepository`, `IRubricRepository`, `IEvaluationRepository` | Repository (порты) | интерфейсы в домене, реализации снаружи |

**Entity vs Value Object — пример.** `Criterion` — сущность: если изменить название, это всё тот же критерий
`#2`, на который ссылаются оценки. `Weight` — Value Object: вес «3» равен любому другому весу «3», у него нет собственной жизни, он неизменяем и проверяется при создании (нельзя создать `Weight(-1)`).

**Границы агрегатов.**
- `Rubric` + `Criterion` — один агрегат: уникальность названий и набор существующих критериев — общая согласованность.
- `Student` — отдельный агрегат.
- `Evaluation` — отдельный агрегат, ссылается на студента и критерий **по идентификатору**, а не по объекту. Оценок много и тянуть их вместе со студентом или рубрикатором при каждой операции нет смысла. Согласованность между агрегатами обеспечивает сценарий приложения (проверка существования студента) и фабрика (проверка рубрикатора).

Модель не анемичная: сеттеры закрыты, изменения идут через методы с инвариантами (`AddCriterion`, `Correct`), а не через «сервисы-проходники» над DTO.

## Архитектура: Ports and Adapters (Hexagonal)

```
 ConsoleApp (UI-адаптер + composition root, DI-контейнер)
        │ зависит от
        ▼
 Infrastructure (адаптеры: JSON-хранилище, CSV/JSON-экспорт)
        │ реализует порты из
        ▼
 Application (use cases, фасад, DTO, порт IResultExporter)
        │ использует
        ▼
 Domain (entity, VO, инварианты, domain service, порты репозиториев)
```

Стрелки зависимостей идут **внутрь**: `ConsoleApp → Infrastructure → Application → Domain`. Домен не ссылается ни на что, кроме BCL.

| Проект | Роль |
|---|---|
| `Pos.Domain` | Entities, VO, `Rubric`, `EvaluationFactory`, стратегии расчёта, `FinalGradeCalculator`, порты репозиториев |
| `Pos.Application` | Use cases (`StudentUseCases`, `RubricUseCases`, `EvaluationUseCases`, `ResultsUseCases`), `PosFacade`, DTO, порт `IResultExporter`, `GradingPolicy` |
| `Pos.Infrastructure` | `Json*Repository` (адаптеры портов домена), `CsvResultExporter` / `JsonResultExporter` (адаптеры порта приложения) |
| `Pos.ConsoleApp` | Разбор команд, вывод, `CompositionRoot` |
| `tests/Pos.Tests` | Тесты инвариантов, Domain Service, сценариев и хранилища |

**DI.** Граф собирает `CompositionRoot` с `Microsoft.Extensions.DependencyInjection`: порты связываются с адаптерами в одном месте, на краю приложения. 
Внутренние слои контейнера не знают (в тестах граф собирается вручную).

## Паттерны GoF

Выбрано пять паттернов — по одному-два на группу, каждый закрывает конкретную боль.

### Порождающие

**Factory — `EvaluationFactory`** (`Pos.Domain/Factories`).
*Боль:* оценку нельзя создавать как угодно: нужно проверить, что критерий есть в рубрикаторе, что балл в диапазоне,и проставить время. 
*Решение:* точка создания новой оценки с проверками; прямой конструктор недоступен другим слоям.
При восстановлении из JSON метод `Restore` повторно проверяет критерий по рубрикатору и диапазон балла. 
Время берётся из `TimeProvider`, поэтому фабрика тестируема.

### Структурные

**Decorator — `RoundingGradingStrategy`** (`Pos.Domain/Grading`).
*Боль:* «особенности подсчёта» (округление и т.п.) не должны быть вшиты в каждое правило и не должны порождать подклассы вида «строгое с округлением», «мягкое с округлением». 
*Решение:* декоратор оборачивает любую `IGradingStrategy`; `GradingPolicy` включает его по настройке `policy round on|off`.

**Facade — `PosFacade`** (`Pos.Application`).
*Боль:* для одного пользовательского действия консоли пришлось бы знать несколько use case-классов и их зависимости. 
*Решение:* единая точка входа в сценарии; UI зависит от одного класса, а состав сценариев
можно менять, не трогая консоль.

### Поведенческие

**Strategy — `IGradingStrategy`** (`WeightedAverageStrategy`, `GradedOnlyWeightedAverageStrategy`).
*Боль:* правило итога у вуза может меняться (что делать с неоценённым критерием: считать нулём или игнорировать).
*Решение:* правило вынесено в сменяемую стратегию, `FinalGradeCalculator` не знает формулы. Переключается в рантайме:
`policy mode strict|lenient` (в демо у студента без оценки по семинару итог меняется с 4.3 на 5.4).

**Template Method — `ResultExporterBase`** (`Pos.Infrastructure/Export`).
*Боль:* форматы выгрузки (CSV, JSON) совпадают по шагам «подготовить строки → сериализовать → записать файл» и отличаются только сериализацией. 
*Решение:* общий каркас в базовом классе, подклассы переопределяют `Serialize`.
Новый формат — один новый класс и одна строка в DI.

## Тесты

`tests/Pos.Tests` — 25 тестов: инварианты VO, фабрики и `Evaluation.Correct`; `FinalGradeCalculator` для обеих стратегий и граничных случаев; декоратор округления; сценарии приложения; сохранение в JSON и экспорт.

Запуск: `dotnet run --project tests/Pos.Tests` (код возврата ненулевой, если есть упавшие тесты).
Тесты написаны на минимальном собственном раннере (`Check.cs`). 
