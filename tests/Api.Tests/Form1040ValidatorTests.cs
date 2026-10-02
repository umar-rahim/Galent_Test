using Api.Models;
using Api.Services;
using Xunit;

namespace Api.Tests;

public sealed class Form1040ValidatorTests
{
    private readonly Form1040Validator _validator = new();

    [Fact]
    public void Validate_ReturnsNoFindingsForValidBasicReturn()
    {
        var findings = _validator.Validate(new Form1040Data
        {
            TaxpayerFirstName = "Alex",
            TaxpayerLastName = "Example",
            TaxpayerSsn = "123-45-6789",
            FilingStatus = FilingStatus.Single,
            ZipCode = "12345"
        });

        Assert.Empty(findings);
    }

    [Fact]
    public void Validate_ReportsRequiredIdentityAndFilingStatus()
    {
        var findings = _validator.Validate(new Form1040Data());
        var codes = findings.Select(item => item.Code).ToHashSet();

        Assert.Contains("REQUIRED_NAME", codes);
        Assert.Contains("INVALID_SSN", codes);
        Assert.Contains("REQUIRED_FILING_STATUS", codes);
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData("12-345-6789")]
    [InlineData("")]
    public void Validate_RejectsMalformedSsn(string ssn)
    {
        var form = ValidForm();
        form.TaxpayerSsn = ssn;

        Assert.Contains(_validator.Validate(form), item => item.Code == "INVALID_SSN");
    }

    [Fact]
    public void Validate_RequiresSpouseSsnForMarriedFilingSeparately()
    {
        var form = ValidForm();
        form.FilingStatus = FilingStatus.MarriedFilingSeparately;

        Assert.Contains(_validator.Validate(form), item => item.Code == "MFS_SPOUSE_SSN_REQUIRED");
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("12345-123")]
    [InlineData("ABCDE")]
    public void Validate_RejectsInvalidZip(string zip)
    {
        var form = ValidForm();
        form.ZipCode = zip;

        Assert.Contains(_validator.Validate(form), item => item.Code == "INVALID_ZIP");
    }

    [Fact]
    public void Validate_RejectsMoreThanFourDependents()
    {
        var form = ValidForm();
        form.Dependents = Enumerable.Range(0, 5).Select(_ => new Form1040Dependent
        {
            FirstName = "Kid", LastName = "Example", Ssn = "123-45-6789"
        }).ToList();

        Assert.Contains(_validator.Validate(form), item => item.Code == "TOO_MANY_LISTED_DEPENDENTS");
    }

    [Fact]
    public void Validate_ReportsDependentNameSsnAndCreditConflict()
    {
        var form = ValidForm();
        form.Dependents.Add(new Form1040Dependent
        {
            FirstName = "", LastName = "Example", Ssn = "invalid",
            QualifiesForChildTaxCredit = true, QualifiesForOtherDependentCredit = true
        });

        var codes = _validator.Validate(form).Select(item => item.Code).ToHashSet();

        Assert.Contains("DEPENDENT_NAME_REQUIRED", codes);
        Assert.Contains("DEPENDENT_SSN_REQUIRED", codes);
        Assert.Contains("DEPENDENT_CREDIT_CONFLICT", codes);
    }

    [Theory]
    [InlineData(null, null, null, false)]
    [InlineData("021000021", "12345678", BankAccountType.Checking, false)]
    [InlineData("021000021", "12345678", null, true)]
    [InlineData("123456789", "12345678", BankAccountType.Checking, true)]
    [InlineData("021000021", "12", BankAccountType.Checking, true)]
    public void Validate_AppliesBankDetailRules(string? routing, string? account, BankAccountType? type, bool expectError)
    {
        var form = ValidForm();
        form.RoutingNumber = routing;
        form.AccountNumber = account;
        form.BankAccountType = type;

        var findings = _validator.Validate(form);

        Assert.Equal(expectError, findings.Any(item => item.Code is "BANK_DETAILS_INCOMPLETE" or "INVALID_ROUTING_NUMBER" or "INVALID_ACCOUNT_NUMBER"));
    }

    [Fact]
    public void Validate_RequiresThirdPartyDesigneeDetailsWhenSelected()
    {
        var form = ValidForm();
        form.ThirdPartyDesignee = true;

        Assert.Contains(_validator.Validate(form), item => item.Code == "DESIGNEE_DETAILS_REQUIRED");
    }

    [Theory]
    [InlineData(12.345m)]
    [InlineData(100_000_000_000m)]
    public void Validate_RejectsMoneyWithTooManyDecimalsOrAboveLimit(decimal amount)
    {
        var form = ValidForm();
        form.Line1a = amount;

        Assert.Contains(_validator.Validate(form), item => item.Code == "INVALID_MONEY");
    }

    [Fact]
    public void Validate_ReportsRefundAndCarryForwardInconsistencies()
    {
        var form = ValidForm();
        form.Line35a = 100m;
        form.Line36 = 150m;
        form.Line34 = 100m;
        form.Line37 = 50m;

        var codes = _validator.Validate(form).Select(item => item.Code).ToHashSet();

        Assert.Contains("REFUND_AND_AMOUNT_OWED_CONFLICT", codes);
        Assert.Contains("REFUND_EXCEEDS_OVERPAYMENT", codes);
        Assert.Contains("CARRY_FORWARD_EXCEEDS_OVERPAYMENT", codes);
    }

    [Fact]
    public void Validate_ThrowsWhenFormIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => _validator.Validate(null!));
    }

    private static Form1040Data ValidForm() => new()
    {
        TaxpayerFirstName = "Alex",
        TaxpayerLastName = "Example",
        TaxpayerSsn = "123-45-6789",
        FilingStatus = FilingStatus.Single
    };
}
