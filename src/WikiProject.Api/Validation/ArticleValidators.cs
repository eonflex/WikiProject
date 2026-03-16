using FluentValidation;
using WikiProject.Api.DTOs;

namespace WikiProject.Api.Validation;

public class CreateArticleRequestValidator : AbstractValidator<CreateArticleRequest>
{
    public CreateArticleRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must be 200 characters or fewer.");

        RuleFor(x => x.Slug)
            .MaximumLength(200).WithMessage("Slug must be 200 characters or fewer.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be lowercase alphanumeric with hyphens.")
            .When(x => !string.IsNullOrEmpty(x.Slug));

        RuleFor(x => x.Summary)
            .NotEmpty().WithMessage("Summary is required.")
            .MaximumLength(500).WithMessage("Summary must be 500 characters or fewer.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(100).WithMessage("Category must be 100 characters or fewer.");

        RuleFor(x => x.Tags)
            .Must(tags => tags == null || tags.All(t => t.Length <= 50))
            .WithMessage("Each tag must be 50 characters or fewer.");
    }
}

public class UpdateArticleRequestValidator : AbstractValidator<UpdateArticleRequest>
{
    public UpdateArticleRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must be 200 characters or fewer.");

        RuleFor(x => x.Slug)
            .MaximumLength(200).WithMessage("Slug must be 200 characters or fewer.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be lowercase alphanumeric with hyphens.")
            .When(x => !string.IsNullOrEmpty(x.Slug));

        RuleFor(x => x.Summary)
            .NotEmpty().WithMessage("Summary is required.")
            .MaximumLength(500).WithMessage("Summary must be 500 characters or fewer.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(100).WithMessage("Category must be 100 characters or fewer.");

        RuleFor(x => x.Tags)
            .Must(tags => tags == null || tags.All(t => t.Length <= 50))
            .WithMessage("Each tag must be 50 characters or fewer.");
    }
}
