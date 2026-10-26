using System.Collections.Generic;

namespace Core.Dto;

public record OrderDto(
    string Id,
    string CustomerId,
    bool IsConfirmed,
    IReadOnlyList<OrderLineDto> Lines,
    string? Status = null);