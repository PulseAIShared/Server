using FluentValidation;

namespace Application.Users.Register;

internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty();
        RuleFor(c => c.LastName).NotEmpty();
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
        RuleFor(c => c.Password).NotEmpty().MinimumLength(8);
        RuleFor(c => c.CompanyName)
       .NotEmpty()
       .MaximumLength(200);

        RuleFor(c => c.CompanyDomain)
            .Must(BeValidDomain)
            .When(c => !string.IsNullOrWhiteSpace(c.CompanyDomain))
            .WithMessage("Please enter a valid domain (e.g., company.com)");

        RuleFor(c => c.CompanyCountry)
            .NotEmpty()
            .Length(2)
            .WithMessage("Country must be a 2-letter code (e.g., US, UK, CA)");
    }

    private static bool BeValidDomain(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) return true;
        return domain.Contains('.') &&
               !domain.Contains(' ') &&
               domain.Length > 3 &&
               !domain.StartsWith('.') &&
               !domain.EndsWith('.');
    }
}

