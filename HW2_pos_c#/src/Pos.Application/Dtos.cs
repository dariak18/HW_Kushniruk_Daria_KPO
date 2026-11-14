namespace Pos.Application;

// DTO живут только на границе приложения: консоль и экспорт видят их, а не доменные объекты

public sealed record StudentDto(int Id, string FullName);

public sealed record CriterionDto(int Id, string Name, decimal MaxScore, decimal Weight);

public sealed record EvaluationDto(
    int StudentId,
    int CriterionId,
    string CriterionName,
    decimal Score,
    decimal MaxScore,
    string? Comment,
    DateTime EvaluatedAtUtc);

public sealed record SetGradeResult(EvaluationDto Evaluation, bool WasCorrection);

public sealed record FinalGradeDto(
    int StudentId,
    string StudentName,
    decimal Value,
    int GradedCriteria,
    int TotalCriteria,
    string Rule);
