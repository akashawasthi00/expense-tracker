using ExpenseTracker.Application.Contracts.Expenses;
using ExpenseTracker.Domain.Enums;
using FluentValidation;

namespace ExpenseTracker.Application.Validation;

public sealed class CreateExpenseRequestValidator : AbstractValidator<CreateExpenseRequest>
{
    public CreateExpenseRequestValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(1_000_000);

        // Exact/percentage splits need explicit participants (Equal can default to all members).
        RuleFor(x => x.Participants)
            .NotEmpty()
            .When(x => x.GroupId is not null && x.SplitType != SplitType.Equal)
            .WithMessage("Exact and percentage splits require a list of participants.");

        RuleForEach(x => x.Participants)
            .ChildRules(p => p.RuleFor(i => i.UserId).NotEmpty())
            .When(x => x.Participants is not null);
    }
}

public sealed class UpdateExpenseRequestValidator : AbstractValidator<UpdateExpenseRequest>
{
    public UpdateExpenseRequestValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(1_000_000);
        RuleFor(x => x.Date).NotEmpty();

        RuleForEach(x => x.Participants)
            .ChildRules(p => p.RuleFor(i => i.UserId).NotEmpty())
            .When(x => x.Participants is not null);
    }
}
