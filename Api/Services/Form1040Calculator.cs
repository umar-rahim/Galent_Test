using Api.Models;

namespace Api.Services;

public sealed class Form1040Calculator : IForm1040Calculator
{
    public decimal GetStandardDeduction(FilingStatus filingStatus) => filingStatus switch
    {
        FilingStatus.MarriedFilingJointly or FilingStatus.QualifyingSurvivingSpouse => 31_500m,
        FilingStatus.HeadOfHousehold => 23_625m,
        FilingStatus.Single or FilingStatus.MarriedFilingSeparately => 15_750m,
        _ => throw new ArgumentOutOfRangeException(nameof(filingStatus))
    };

    public void Recalculate(Form1040Data form)
    {
        ArgumentNullException.ThrowIfNull(form);
        form.Line1z = Sum(form.Line1a, form.Line1b, form.Line1c, form.Line1d, form.Line1e, form.Line1f, form.Line1g, form.Line1h);
        form.Line9 = Sum(form.Line1z, form.Line2b, form.Line3b, form.Line4b, form.Line5b, form.Line6b, form.Line7a, form.Line8);
        form.Line11a = form.Line9 - Value(form.Line10);

        if (form.FilingStatus.HasValue)
        {
            form.Line12 ??= GetStandardDeduction(form.FilingStatus.Value);
        }

        form.Line14 = Sum(form.Line12, form.Line13a, form.Line13b);
        form.Line15 = Math.Max(0m, form.Line11a - Value(form.Line14));
        form.Line24 = Sum(form.Line22, form.Line23);
        form.Line25d = Sum(form.Line25a, form.Line25b, form.Line25c);
        form.Line33 = Sum(form.Line25d, form.Line26, form.Line32);
        form.Line34 = Math.Max(0m, form.Line33 - form.Line24);
        form.Line37 = Math.Max(0m, form.Line24 - form.Line33);
        form.Line35a = Math.Max(0m, form.Line34 - Value(form.Line36));
    }

    private static decimal Sum(params decimal?[] values) => values.Sum(Value);
    private static decimal Value(decimal? value) => value ?? 0m;
}
