using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Api.Models;

public sealed class Form1040Data
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubmissionId { get; set; }
    [JsonIgnore, ValidateNever] public Form1040Submission Submission { get; set; } = null!;
    [StringLength(100)] public string TaxpayerFirstName { get; set; } = string.Empty;
    [StringLength(100)] public string TaxpayerLastName { get; set; } = string.Empty;
    [StringLength(11)] public string TaxpayerSsn { get; set; } = string.Empty;
    [StringLength(100)] public string? SpouseFirstName { get; set; }
    [StringLength(100)] public string? SpouseLastName { get; set; }
    [StringLength(11)] public string? SpouseSsn { get; set; }
    [StringLength(200)] public string AddressLine1 { get; set; } = string.Empty;
    [StringLength(100)] public string? AddressLine2 { get; set; }
    [StringLength(100)] public string City { get; set; } = string.Empty;
    [StringLength(100)] public string? State { get; set; }
    [StringLength(10)] public string? ZipCode { get; set; }
    [StringLength(100)] public string? ForeignCountry { get; set; }
    [StringLength(100)] public string? ForeignProvince { get; set; }
    [StringLength(20)] public string? ForeignPostalCode { get; set; }
    public FilingStatus? FilingStatus { get; set; }
    public bool DigitalAssetsQuestionYes { get; set; }
    public bool MoreThanFourDependents { get; set; }
    public List<Form1040Dependent> Dependents { get; set; } = [];
    public decimal? Line1a { get; set; }
    public decimal? Line1b { get; set; }
    public decimal? Line1c { get; set; }
    public decimal? Line1d { get; set; }
    public decimal? Line1e { get; set; }
    public decimal? Line1f { get; set; }
    public decimal? Line1g { get; set; }
    public decimal? Line1h { get; set; }
    public decimal? Line1i { get; set; }
    public decimal Line1z { get; set; }
    public decimal? Line2a { get; set; }
    public decimal? Line2b { get; set; }
    public decimal? Line3a { get; set; }
    public decimal? Line3b { get; set; }
    public decimal? Line4a { get; set; }
    public decimal? Line4b { get; set; }
    public decimal? Line5a { get; set; }
    public decimal? Line5b { get; set; }
    public decimal? Line6a { get; set; }
    public decimal? Line6b { get; set; }
    public decimal? Line7a { get; set; }
    public decimal? Line8 { get; set; }
    public decimal Line9 { get; set; }
    public decimal? Line10 { get; set; }
    public decimal Line11a { get; set; }
    public decimal? Line11b { get; set; }
    public decimal? Line12 { get; set; }
    public decimal? Line13a { get; set; }
    public decimal? Line13b { get; set; }
    public decimal? Line14 { get; set; }
    public decimal Line15 { get; set; }
    public decimal? Line16 { get; set; }
    public decimal? Line17 { get; set; }
    public decimal? Line18 { get; set; }
    public decimal? Line19 { get; set; }
    public decimal? Line20 { get; set; }
    public decimal? Line21 { get; set; }
    public decimal? Line22 { get; set; }
    public decimal? Line23 { get; set; }
    public decimal Line24 { get; set; }
    public decimal? Line25a { get; set; }
    public decimal? Line25b { get; set; }
    public decimal? Line25c { get; set; }
    public decimal? Line25d { get; set; }
    public decimal? Line26 { get; set; }
    public decimal? Line27 { get; set; }
    public decimal? Line28 { get; set; }
    public decimal? Line29 { get; set; }
    public decimal? Line30 { get; set; }
    public decimal? Line31 { get; set; }
    public decimal? Line32 { get; set; }
    public decimal Line33 { get; set; }
    public decimal Line34 { get; set; }
    public decimal? Line35a { get; set; }
    public string? RoutingNumber { get; set; }
    public BankAccountType? BankAccountType { get; set; }
    public string? AccountNumber { get; set; }
    public decimal? Line36 { get; set; }
    public decimal Line37 { get; set; }
    public decimal? Line38 { get; set; }
    public bool ThirdPartyDesignee { get; set; }
    [StringLength(120)] public string? DesigneeName { get; set; }
    [StringLength(30)] public string? DesigneePhone { get; set; }
    [StringLength(100)] public string? DesigneePin { get; set; }
    [StringLength(120)] public string? TaxpayerOccupation { get; set; }
    [StringLength(20)] public string? TaxpayerIdentityProtectionPin { get; set; }
    public DateOnly? TaxpayerSignatureDate { get; set; }
    [StringLength(120)] public string? SpouseOccupation { get; set; }
    [StringLength(20)] public string? SpouseIdentityProtectionPin { get; set; }
    public DateOnly? SpouseSignatureDate { get; set; }
    [StringLength(120)] public string? PreparerName { get; set; }
    [StringLength(30)] public string? PreparerPhone { get; set; }
    [StringLength(30)] public string? PreparerPtin { get; set; }
    [StringLength(120)] public string? PreparerFirmName { get; set; }
    [StringLength(200)] public string? PreparerFirmAddress { get; set; }
    [StringLength(30)] public string? PreparerFirmEin { get; set; }
    public bool PreparerSelfEmployed { get; set; }
    public DateOnly? PreparerSignatureDate { get; set; }
}

public sealed class Form1040Dependent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [JsonIgnore] public Guid Form1040DataId { get; set; }
    [StringLength(100)] public string FirstName { get; set; } = string.Empty;
    [StringLength(100)] public string LastName { get; set; } = string.Empty;
    [StringLength(11)] public string Ssn { get; set; } = string.Empty;
    [StringLength(50)] public string Relationship { get; set; } = string.Empty;
    public bool QualifiesForChildTaxCredit { get; set; }
    public bool QualifiesForOtherDependentCredit { get; set; }
}

public enum FilingStatus { Single, MarriedFilingJointly, MarriedFilingSeparately, HeadOfHousehold, QualifyingSurvivingSpouse }
public enum BankAccountType { Checking, Savings }
