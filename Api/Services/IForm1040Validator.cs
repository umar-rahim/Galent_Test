using Api.Models;

namespace Api.Services;

public interface IForm1040Validator
{
    IReadOnlyList<ValidationFinding> Validate(Form1040Data form);
}
