using FluentValidation;

namespace Identity.Sso.Application.Behaviors.Authorization.Queries.GetUserProfile;

public class GetUserProfileQueryValidator : AbstractValidator<GetUserProfileQuery>
{
    public GetUserProfileQueryValidator()
    {
        RuleFor(x => x.SubjectId).NotEmpty();
    }
}
