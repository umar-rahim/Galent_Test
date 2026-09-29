using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Api.Models;

namespace Api.Services;

public sealed class Form1040Validator : IForm1040Validator
{
    private static readonly Regex SsnPattern = new(@"^\d{3}-\d{2}-\d{4}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ZipPattern = new(@"^\d{5}(-\d{4})?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex DigitsPattern = new(@"^\d+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private const decimal MaximumMoney = 99_999_999_999.99m;

    public IReadOnlyList<ValidationFinding> Validate(Form1040Data form)
    {
        ArgumentNullException.ThrowIfNull(form);
        var findings = new List<ValidationFinding>();

        if (string.IsNullOrWhiteSpace(form.TaxpayerFirstName) || string.IsNullOrWhiteSpace(form.TaxpayerLastName))
            Add("REQUIRED_NAME", "taxpayer.name", "Taxpayer name is required.");
        if (!IsSsn(form.TaxpayerSsn))
            Add("INVALID_SSN", "taxpayer.ssn", "Enter an SSN in ###-##-#### format.");
        if (!form.FilingStatus.HasValue || !Enum.IsDefined(form.FilingStatus.Value))
            Add("REQUIRED_FILING_STATUS", "filingStatus", "Select a filing status.");
        if (form.FilingStatus == FilingStatus.MarriedFilingSeparately && !IsSsn(form.SpouseSsn))
            Add("MFS_SPOUSE_SSN_REQUIRED", "spouse.ssn", "A valid spouse SSN is required for Married Filing Separately.");
        if (!string.IsNullOrWhiteSpace(form.ZipCode) && !ZipPattern.IsMatch(form.ZipCode))
            Add("INVALID_ZIP", "taxpayer.zipCode", "Enter a 5-digit ZIP code or ZIP+4.");
        if (form.Dependents.Count > 4)
            Add("TOO_MANY_LISTED_DEPENDENTS", "dependents", "List no more than four dependents; use the additional-dependents indicator for others.");

        for (var index = 0; index < form.Dependents.Count; index++)
        {
            var dependent = form.Dependents[index];
            var field = $"dependents[{index}]";
            if (string.IsNullOrWhiteSpace(dependent.FirstName) || string.IsNullOrWhiteSpace(dependent.LastName))
                Add("DEPENDENT_NAME_REQUIRED", $"{field}.name", "Dependent name is required.");
            if (!IsSsn(dependent.Ssn))
                Add("DEPENDENT_SSN_REQUIRED", $"{field}.ssn", "Enter this dependent's SSN in ###-##-#### format.");
            if (dependent.QualifiesForChildTaxCredit && dependent.QualifiesForOtherDependentCredit)
                Add("DEPENDENT_CREDIT_CONFLICT", field, "Select no more than one dependent credit type.");
        }

        ValidateBankDetails();
        ValidateMoney();
        if (form.ThirdPartyDesignee && (string.IsNullOrWhiteSpace(form.DesigneeName) || string.IsNullOrWhiteSpace(form.DesigneePhone)))
            Add("DESIGNEE_DETAILS_REQUIRED", "thirdPartyDesignee", "Enter the designee's name and phone number.");
        if (Value(form.Line35a) > 0 && form.Line37 > 0)
            Add("REFUND_AND_AMOUNT_OWED_CONFLICT", "line35a", "A return cannot claim a refund and an amount owed at the same time.");
        if (Value(form.Line35a) > form.Line34)
            Add("REFUND_EXCEEDS_OVERPAYMENT", "line35a", "The refund cannot exceed the overpayment.");
        if (Value(form.Line36) > form.Line34)
            Add("CARRY_FORWARD_EXCEEDS_OVERPAYMENT", "line36", "The amount applied to next year cannot exceed the overpayment.");

        return findings;

        void Add(string code, string field, string message) => findings.Add(new ValidationFinding
        {
            Code = code,
            Severity = FindingSeverity.Error,
            Field = field,
            Message = message
        });

        void ValidateBankDetails()
        {
            var hasRouting = !string.IsNullOrWhiteSpace(form.RoutingNumber);
            var hasAccount = !string.IsNullOrWhiteSpace(form.AccountNumber);
            if (hasRouting != hasAccount || (hasAccount && !form.BankAccountType.HasValue))
            {
                Add("BANK_DETAILS_INCOMPLETE", "bankAccount", "Provide routing number, account number, and account type together.");
            }
            if (hasRouting && (!DigitsPattern.IsMatch(form.RoutingNumber!) || form.RoutingNumber!.Length != 9 || !IsValidRoutingNumber(form.RoutingNumber)))
            {
                Add("INVALID_ROUTING_NUMBER", "routingNumber", "Enter a valid 9-digit routing number.");
            }
            if (hasAccount && (!DigitsPattern.IsMatch(form.AccountNumber!) || form.AccountNumber!.Length is < 4 or > 17))
            {
                Add("INVALID_ACCOUNT_NUMBER", "accountNumber", "Enter an account number containing 4 to 17 digits.");
            }
        }

        void ValidateMoney()
        {
            foreach (var property in typeof(Form1040Data).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                         .Where(property => property.Name.StartsWith("Line", StringComparison.Ordinal)
                             && (property.PropertyType == typeof(decimal) || property.PropertyType == typeof(decimal?))))
            {
                var value = property.GetValue(form) as decimal?;
                if (!value.HasValue)
                    continue;
                if (Math.Abs(value.Value) > MaximumMoney || decimal.Round(value.Value, 2) != value.Value)
                {
                    var field = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
                    Add("INVALID_MONEY", field, "Enter a monetary amount with no more than two decimal places.");
                }
            }
        }
    }

    private static bool IsSsn(string? value) => !string.IsNullOrWhiteSpace(value) && SsnPattern.IsMatch(value);

    private static bool IsValidRoutingNumber(string value)
    {
        var digits = value.Select(character => character - '0').ToArray();
        var checksum = 3 * (digits[0] + digits[3] + digits[6])
            + 7 * (digits[1] + digits[4] + digits[7])
            + digits[2] + digits[5] + digits[8];
        return checksum % 10 == 0;
    }

    private static decimal Value(decimal? value) => value ?? 0m;
}
