using SharedKernel;

namespace Domain.Users;

public static class UserErrors
{
    public static Error NotFound(Guid userId) => Error.NotFound(
        "Users.NotFound",
        $"The user with the Id = '{userId}' was not found");

    public static Error Unauthorized() => Error.Failure(
        "Users.Unauthorized",
        "You are not authorized to perform this action.");

    public static readonly Error NotFoundByEmail = Error.NotFound(
        "Users.NotFoundByEmail",
        "The user with the specified email was not found");

    public static readonly Error EmailNotUnique = Error.Conflict(
        "Users.EmailNotUnique",
        "The provided email is not unique");

    public static readonly Error RefreshTokenInvalid = Error.Conflict(
        "Users.RefreshTokenInvalid",
        "The refresh token is invalid or expired.");

    public static readonly Error InvalidOrExpiredInvitation = Error.Problem(
     "Users.InvalidOrExpiredInvitation",
     "The invitation token is invalid or has expired");

    public static readonly Error CompanyUserLimitReached = Error.Problem(
        "Users.CompanyUserLimitReached",
        "This company has reached its user limit for the current plan");

    public static readonly Error CompanyInfoOrInvitationRequired = Error.Problem(
        "Users.CompanyInfoOrInvitationRequired",
        "Either company information or invitation token is required");

    public static readonly Error UserNotInCompany = Error.Problem(
        "Users.UserNotInCompany",
        "User is not associated with any company");

    public static readonly Error InsufficientPermissions = Error.Problem(
        "Users.InsufficientPermissions",
        "User does not have sufficient permissions to perform this action");

    public static readonly Error UserAlreadyExists = Error.Problem(
        "Users.UserAlreadyExists",
        "A user with this email address already exists");

    public static readonly Error InvitationAlreadySent = Error.Problem(
        "Users.InvitationAlreadySent",
        "An invitation has already been sent to this email address");
}
