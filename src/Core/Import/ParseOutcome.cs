using Core.Dto;

namespace Core.Import;

internal abstract record ParseOutcome;

internal sealed record ParseOk(ImportRowDto Value) : ParseOutcome;

internal sealed record ParseFailed(string Reason) : ParseOutcome;