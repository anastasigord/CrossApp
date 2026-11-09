using System.Collections.Generic;
using System.Linq;
using Core.Domain;
using Core.Reports;
using Xunit;

namespace Core.Tests;

public class ReportServiceTests
{
    [Theory] // 7) звіт на кількох наборах
    [InlineData(1000, 2)]
    [InlineData(100, 1)]
    [InlineData(50, 0)]
    public void LowValueOrders_CountsOnlyBelowThreshold(int threshold, int expected) =>
        Assert.Equal(expected, ReportService.LowValueOrders(TestData.Items(), threshold).Count);

    [Fact] // 8) детермінований топ-N: порядок не залежить від порядку джерела
    public void TopByTotal_IsDeterministic()
    {
        var expected = new[] { "T-002", "T-004", "T-003" };

        var forward = ReportService.TopByTotal(TestData.Items(), 3).Select(l => l.Id);
        var reversed = ReportService.TopByTotal(TestData.Items().AsEnumerable().Reverse(), 3).Select(l => l.Id);

        Assert.Equal(expected, forward);
        Assert.Equal(expected, reversed);
    }

    [Fact] // 9) порожнє джерело не ламає агрегати
    public void Totals_OnEmptySource_ReturnsZeros() =>
        Assert.Equal((0, 0m, 0m, 0m), ReportService.Totals(new List<Order>()));

    [Fact] // 10) групування за статусом
    public void ByStatus_GroupsAndSums()
    {
        var groups = ReportService.ByStatus(TestData.Items()).ToDictionary(g => g.Key);

        Assert.Equal(2, groups["Draft"].Count);
        Assert.Equal(750m, groups["Draft"].TotalAmount);
        Assert.Equal(1, groups["Confirmed"].Count);
        Assert.Equal(1, groups["Cancelled"].Count);
    }
}