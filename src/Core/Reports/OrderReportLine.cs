namespace Core.Reports;

public sealed record OrderReportLine(string Id, string CustomerId, decimal Total, string Status, int LinesCount);