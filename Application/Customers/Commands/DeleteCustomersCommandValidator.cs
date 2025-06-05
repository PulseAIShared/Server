using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Customers.Commands
{
    internal sealed class DeleteCustomersCommandValidator : AbstractValidator<DeleteCustomersCommand>
    {
        public DeleteCustomersCommandValidator()
        {
            RuleFor(c => c.CustomerIds)
                .NotNull()
                .NotEmpty()
                .WithMessage("At least one customer ID must be provided");

            RuleFor(c => c.CustomerIds)
                .Must(ids => ids.Count <= 100)
                .WithMessage("Cannot delete more than 100 customers at once")
                .When(c => c.CustomerIds != null);

            RuleForEach(c => c.CustomerIds)
                .NotEmpty()
                .WithMessage("Customer ID cannot be empty");

            RuleFor(c => c.CustomerIds)
                .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("Duplicate customer IDs are not allowed")
                .When(c => c.CustomerIds != null && c.CustomerIds.Any());
        }
    }
}
