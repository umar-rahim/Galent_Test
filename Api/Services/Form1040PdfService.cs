using System.Globalization;
using Api.Models;
using Microsoft.Extensions.Options;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;

namespace Api.Services;

public sealed class Form1040PdfOptions
{
    public string TemplatePath { get; set; } = "assets/f1040.pdf";
}

public sealed class Form1040PdfService : IForm1040PdfService
{
    private const string Root = "topmostSubform[0]";
    private readonly string _templatePath;

    public Form1040PdfService(IOptions<Form1040PdfOptions> options, IHostEnvironment environment)
    {
        _templatePath = Path.GetFullPath(options.Value.TemplatePath, environment.ContentRootPath);
    }

    public Task<Stream> GeneratePdfAsync(Form1040Submission submission)
    {
        ArgumentNullException.ThrowIfNull(submission);
        if (!File.Exists(_templatePath))
            throw new FileNotFoundException("The bundled 2025 Form 1040 PDF template was not found.", _templatePath);

        using var document = PdfReader.Open(_templatePath, PdfDocumentOpenMode.Modify);
        var acroForm = document.AcroForm ?? throw new InvalidDataException("The Form 1040 template does not contain an AcroForm.");
        var fields = new Dictionary<string, PdfDictionary>(StringComparer.Ordinal);
        foreach (var item in acroForm.Fields)
            CollectFields(item, string.Empty, fields);

        string Page1(string field) => $"{Root}.Page1[0].{field}";
        string Page2(string field) => $"{Root}.Page2[0].{field}";
        void Text(string field, string? value) => SetText(fields, field, value ?? string.Empty);
        void P1(string field, string? value) => Text(Page1($"{field}[0]"), value);
        void P2(string field, string? value) => Text(Page2($"{field}[0]"), value);
        void Money1(string field, decimal? value) => P1(field, Money(value));
        void Money2(string field, decimal? value) => P2(field, Money(value));
        void Check(string field, bool checkedValue, string onValue = "/1") => SetCheck(fields, field, checkedValue, onValue);

        var form = submission.Form;
        P1("f1_14", form.TaxpayerFirstName);
        P1("f1_15", form.TaxpayerLastName);
        P1("f1_16", form.TaxpayerSsn);
        P1("f1_17", form.SpouseFirstName);
        P1("f1_18", form.SpouseLastName);
        P1("f1_19", form.SpouseSsn);
        Text(Page1($"Address_ReadOrder[0].f1_20[0]"), form.AddressLine1);
        Text(Page1($"Address_ReadOrder[0].f1_21[0]"), form.AddressLine2);
        Text(Page1($"Address_ReadOrder[0].f1_22[0]"), form.City);
        Text(Page1($"Address_ReadOrder[0].f1_23[0]"), form.State);
        Text(Page1($"Address_ReadOrder[0].f1_24[0]"), form.ZipCode);
        Text(Page1($"Address_ReadOrder[0].f1_25[0]"), form.ForeignCountry);
        Text(Page1($"Address_ReadOrder[0].f1_26[0]"), form.ForeignProvince);
        Text(Page1($"Address_ReadOrder[0].f1_27[0]"), form.ForeignPostalCode);

        Check($"{Root}.Page1[0].Checkbox_ReadOrder[0].c1_8[0]", form.FilingStatus == FilingStatus.Single, "/1");
        Check($"{Root}.Page1[0].Checkbox_ReadOrder[0].c1_8[1]", form.FilingStatus == FilingStatus.MarriedFilingJointly, "/2");
        Check($"{Root}.Page1[0].Checkbox_ReadOrder[0].c1_8[2]", form.FilingStatus == FilingStatus.MarriedFilingSeparately, "/3");
        Check($"{Root}.Page1[0].c1_8[0]", form.FilingStatus == FilingStatus.HeadOfHousehold, "/4");
        Check($"{Root}.Page1[0].c1_8[1]", form.FilingStatus == FilingStatus.QualifyingSurvivingSpouse, "/5");
        Check(Page1("c1_10[0]"), form.DigitalAssetsQuestionYes);
        Check(Page1("c1_10[1]"), !form.DigitalAssetsQuestionYes);
        Check($"{Root}.Page1[0].Dependents_ReadOrder[0].c1_11[0]", form.MoreThanFourDependents);

        for (var index = 0; index < Math.Min(form.Dependents.Count, 4); index++)
        {
            var dependent = form.Dependents[index];
            var number = index + 1;
            var firstField = 31 + index;
            P1($"f1_{firstField:00}", dependent.FirstName);
            P1($"f1_{firstField + 4:00}", dependent.LastName);
            P1($"f1_{firstField + 8:00}", dependent.Ssn);
            P1($"f1_{firstField + 12:00}", dependent.Relationship);
            var creditPath = $"{Root}.Page1[0].Table_Dependents[0].Row7[0].Dependent{number}[0].c1_{27 + number}[0]";
            Check(creditPath, dependent.QualifiesForChildTaxCredit);
            Check(creditPath.Replace($"c1_{27 + number}[0]", $"c1_{27 + number}[1]"), dependent.QualifiesForOtherDependentCredit, "/2");
        }

        Money1("f1_47", form.Line1a);
        Money1("f1_48", form.Line1b);
        Money1("f1_49", form.Line1c);
        Money1("f1_50", form.Line1d);
        Money1("f1_51", form.Line1e);
        Money1("f1_52", form.Line1f);
        Money1("f1_53", form.Line1g);
        Money1("f1_55", form.Line1h);
        Money1("f1_56", form.Line1i);
        Money1("f1_57", form.Line1z);
        Money1("f1_58", form.Line2a);
        Money1("f1_59", form.Line2b);
        Money1("f1_60", form.Line3a);
        Money1("f1_61", form.Line3b);
        Money1("f1_62", form.Line4a);
        Money1("f1_63", form.Line4b);
        Money1("f1_65", form.Line5a);
        Money1("f1_66", form.Line5b);
        Money1("f1_68", form.Line6a);
        Money1("f1_69", form.Line6b);
        Money1("f1_70", form.Line7a);
        Money1("f1_72", form.Line8);
        Money1("f1_73", form.Line9);
        Money1("f1_74", form.Line10);
        Money1("f1_75", form.Line11a);

        Money2("f2_01", form.Line11a);
        Money2("f2_02", form.Line12);
        Money2("f2_03", form.Line13a);
        Money2("f2_04", form.Line13b);
        Money2("f2_05", form.Line14);
        Money2("f2_06", form.Line15);
        Money2("f2_08", form.Line16);
        Money2("f2_09", form.Line17);
        Money2("f2_10", form.Line18);
        Money2("f2_11", form.Line19);
        Money2("f2_12", form.Line20);
        Money2("f2_13", form.Line21);
        Money2("f2_14", form.Line22);
        Money2("f2_15", form.Line23);
        Money2("f2_16", form.Line24);
        Money2("f2_17", form.Line25a);
        Money2("f2_18", form.Line25b);
        Money2("f2_19", form.Line25c);
        Money2("f2_20", form.Line25d);
        Money2("f2_21", form.Line26);
        Money2("f2_23", form.Line27);
        Money2("f2_24", form.Line28);
        Money2("f2_25", form.Line29);
        Money2("f2_26", form.Line30);
        Money2("f2_27", form.Line31);
        Money2("f2_28", form.Line32);
        Money2("f2_29", form.Line33);
        Money2("f2_30", form.Line34);
        Money2("f2_31", form.Line35a);
        Money2("f2_34", form.Line36);
        Money2("f2_35", form.Line37);
        Money2("f2_36", form.Line38);

        Text(Page2("RoutingNo[0].f2_32[0]"), form.RoutingNumber);
        Text(Page2("AccountNo[0].f2_33[0]"), form.AccountNumber);
        Check(Page2("c2_16[0]"), form.BankAccountType == BankAccountType.Checking);
        Check(Page2("c2_16[1]"), form.BankAccountType == BankAccountType.Savings, "/2");
        Text(Page2("f2_37[0]"), form.DesigneeName);
        Text(Page2("f2_38[0]"), form.DesigneePhone);
        Text(Page2("f2_39[0]"), form.DesigneePin);
        Check(Page2("c2_17[0]"), form.ThirdPartyDesignee);
        Check(Page2("c2_17[1]"), !form.ThirdPartyDesignee, "/2");
        Text(Page2("f2_40[0]"), form.TaxpayerOccupation);
        Text(Page2("f2_41[0]"), form.TaxpayerIdentityProtectionPin);
        Text(Page2("f2_42[0]"), form.SpouseOccupation);
        Text(Page2("f2_43[0]"), form.SpouseIdentityProtectionPin);
        Text(Page2("f2_46[0]"), form.PreparerName);
        Text(Page2("f2_47[0]"), form.PreparerPtin);
        Text(Page2("f2_48[0]"), form.PreparerFirmName);
        Text(Page2("f2_49[0]"), form.PreparerPhone);
        Text(Page2("f2_50[0]"), form.PreparerFirmAddress);
        Text(Page2("f2_51[0]"), form.PreparerFirmEin);
        Check(Page2("c2_18[0]"), form.PreparerSelfEmployed);

        acroForm.Elements.SetBoolean("/NeedAppearances", true);
        var output = new MemoryStream();
        document.Save(output);
        output.Position = 0;
        return Task.FromResult<Stream>(output);
    }

    private static void CollectFields(PdfItem? item, string parentPath, IDictionary<string, PdfDictionary> fields)
    {
        while (item is PdfReference reference)
            item = reference.Value;
        if (item is not PdfDictionary dictionary)
            return;

        var partialName = dictionary.Elements.GetString("/T");
        var path = string.IsNullOrEmpty(parentPath) ? partialName : $"{parentPath}.{partialName}";
        if (!string.IsNullOrEmpty(path) && dictionary.Elements.GetName("/Subtype") == "/Widget")
            fields[path] = dictionary;

        if (dictionary.Elements["/Kids"] is PdfArray children)
            foreach (var child in children)
                CollectFields(child, path, fields);
    }

    private static void SetText(IReadOnlyDictionary<string, PdfDictionary> fields, string path, string value)
    {
        if (!fields.TryGetValue(path, out var field))
            throw new InvalidDataException($"The bundled Form 1040 PDF is missing the expected field '{path}'.");
        field.Elements.SetString("/V", value);
    }

    private static void SetCheck(IReadOnlyDictionary<string, PdfDictionary> fields, string path, bool isChecked, string onValue = "/1")
    {
        if (!fields.TryGetValue(path, out var field))
            throw new InvalidDataException($"The bundled Form 1040 PDF is missing the expected checkbox '{path}'.");
        var value = isChecked ? onValue : "/Off";
        field.Elements.SetName("/V", value);
        field.Elements.SetName("/AS", value);
    }

    private static string Money(decimal? value) => value?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty;
}