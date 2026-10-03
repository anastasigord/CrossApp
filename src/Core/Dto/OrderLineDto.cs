namespace Core.Dto;

public record OrderLineDto(string ProductId, string Name, decimal Price, int Quantity);