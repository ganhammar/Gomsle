using OpenIddict.AmazonDynamoDB;

namespace Gomsle.Api.Features.Application;

public static class ApplicationDtoMapper
{
    public static ApplicationDto ToDto(
        OpenIddictDynamoDbApplication application,
        ApplicationConfigurationModel applicationConfiguration) => new ApplicationDto
        {
            AccountId = applicationConfiguration.AccountId,
            AutoProvision = applicationConfiguration.AutoProvision,
            ClientId = application.ClientId,
            DefaultOrigin = applicationConfiguration.DefaultOrigin,
            DisplayName = application.DisplayName,
            EnableProvision = applicationConfiguration.EnableProvision,
            Id = application.Id,
            OidcProviders = applicationConfiguration.OidcProviders,
            Origins = applicationConfiguration.Origins,
            PostLogoutRedirectUris = application.PostLogoutRedirectUris,
            RedirectUris = application.RedirectUris,
        };
}