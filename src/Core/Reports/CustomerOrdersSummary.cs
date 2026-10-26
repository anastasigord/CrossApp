namespace Core.Reports;

public sealed record CustomerOrdersSummary(string CustomerId, string Name, int OrdersCount, decimal TotalSpent);