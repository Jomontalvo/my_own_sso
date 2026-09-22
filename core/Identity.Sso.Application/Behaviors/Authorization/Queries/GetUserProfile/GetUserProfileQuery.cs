using Identity.Sso.Application.Models;
using Identity.Sso.Application.Utils.Mediator;

namespace Identity.Sso.Application.Behaviors.Authorization.Queries.GetUserProfile;

public record GetUserProfileQuery(string SubjectId) : IRequest<UserProfile?>;
