using Api.Models;

namespace Api.Services;

public interface IForm1040Calculator
{
    decimal GetStandardDeduction(FilingStatus filingStatus);
    void Recalculate(Form1040Data form);
}
