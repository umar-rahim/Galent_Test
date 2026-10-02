using Api.Models;
using Api.Services;
using Xunit;

namespace Api.Tests;

public sealed class Form1040CalculatorTests
{
    private readonly Form1040Calculator _calculator = new();

    [Theory]
    [InlineData(FilingStatus.Single, 15750)]
    [InlineData(FilingStatus.MarriedFilingSeparately, 15750)]
    [InlineData(FilingStatus.MarriedFilingJointly, 31500)]
    [InlineData(FilingStatus.QualifyingSurvivingSpouse, 31500)]
    [InlineData(FilingStatus.HeadOfHousehold, 23625)]
    public void GetStandardDeduction_ReturnsAmountForFilingStatus(FilingStatus status, decimal expected)
    {
        Assert.Equal(expected, _calculator.GetStandardDeduction(status));
    }

    [Fact]
    public void GetStandardDeduction_RejectsUndefinedStatus()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.GetStandardDeduction((FilingStatus)999));
    }

    [Fact]
    public void Recalculate_SumsIncomeAndTaxesAndCapsNegativeResultsAtZero()
    {
        var form = new Form1040Data
        {
            FilingStatus = FilingStatus.Single,
            Line1a = 50_000m,
            Line1b = 1_000m,
            Line2b = 200m,
            Line3b = 300m,
            Line8 = 500m,
            Line10 = 1_000m,
            Line12 = 15_750m,
            Line13a = 250m,
            Line13b = 100m,
            Line22 = 3_000m,
            Line23 = 200m,
            Line25a = 2_000m,
            Line25b = 100m,
            Line25c = 50m,
            Line26 = 500m,
            Line32 = 300m,
            Line36 = 100m
        };

        _calculator.Recalculate(form);

        Assert.Equal(51_000m, form.Line1z);
        Assert.Equal(52_000m, form.Line9);
        Assert.Equal(51_000m, form.Line11a);
        Assert.Equal(16_100m, form.Line14);
        Assert.Equal(34_900m, form.Line15);
        Assert.Equal(3_200m, form.Line24);
        Assert.Equal(2_150m, form.Line25d);
        Assert.Equal(2_950m, form.Line33);
        Assert.Equal(0m, form.Line34);
        Assert.Equal(250m, form.Line37);
        Assert.Equal(0m, form.Line35a);
    }

    [Fact]
    public void Recalculate_UsesStandardDeductionOnlyWhenLine12IsMissing()
    {
        var form = new Form1040Data { FilingStatus = FilingStatus.HeadOfHousehold, Line1a = 40_000m };

        _calculator.Recalculate(form);

        Assert.Equal(23_625m, form.Line12);
        Assert.Equal(16_375m, form.Line15);
    }

    [Fact]
    public void Recalculate_LeavesDeductionMissingWithoutFilingStatusAndTreatsMissingInputsAsZero()
    {
        var form = new Form1040Data();

        _calculator.Recalculate(form);

        Assert.Null(form.Line12);
        Assert.Equal(0m, form.Line1z);
        Assert.Equal(0m, form.Line9);
        Assert.Equal(0m, form.Line11a);
        Assert.Equal(0m, form.Line14);
        Assert.Equal(0m, form.Line15);
        Assert.Equal(0m, form.Line24);
        Assert.Equal(0m, form.Line25d);
        Assert.Equal(0m, form.Line33);
        Assert.Equal(0m, form.Line34);
        Assert.Equal(0m, form.Line37);
        Assert.Equal(0m, form.Line35a);
    }

    [Fact]
    public void Recalculate_ThrowsWhenFormIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => _calculator.Recalculate(null!));
    }
}
