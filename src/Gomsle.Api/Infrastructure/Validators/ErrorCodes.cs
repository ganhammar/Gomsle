namespace Gomsle.Api.Infrastructure.Validators;

public static class ErrorCodes
{
    public const string MisingRoleForAccount = "User is not authorized to perform the requested action";
    public const string NotAuthenticated = "User is not authorized to perform the requested action";
    public const string NotAuthorized = "User is not authorized to perform the requested action";
    public const string InvalidUri = "Provided value is not a valid URI";
    public const string DuplicateOrigin = "All origins must be unique, including default origin";
    public const string OnlyOneOwner = "There can be only one owner";
    public const string ResponseTypeIsInvalid = "The specified value for response type is invalid";
}