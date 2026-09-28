namespace Core.Dto;

public abstract record ImportRowDto(string Id);

public sealed record ProductDto(string Id, string Name, decimal Price) : ImportRowDto(Id);

public sealed record CustomerDto(string Id, string FullName, string Email) : ImportRowDto(Id);