namespace Gomsle.Api.Features.Application;

public class InternalOptions
{
    public List<AccountOptions> Accounts { get; set; } = new();
}

public class AccountOptions
{
    public string? Name { get; set; }
    public List<CreateCommand.Command>? InternalApplications { get; set; }
}