using OpenIddict.AmazonDynamoDB;

namespace Gomsle.Api.Features.Application;

public static class ApplicationDtoMapper
{
    public static ApplicationDto ToDto(
        OpenIddictDynamoDbApplication application,
        ApplicationConfigurationModel applicationConfiguration,
        List<ApplicationOriginModel> origins) => new ApplicationDto
        {
            AccountId = applicationConfiguration.AccountId,
            AutoProvision = applicationConfiguration.AutoProvision,
            ClientId = application.ClientId,
            DefaultOrigin = origins.FirstOrDefault(x => x.IsDefault)?.Origin,
            DisplayName = application.DisplayName,
            EnableProvision = applicationConfiguration.EnableProvision,
            Id = application.Id,
            Origins = origins
                .Where(x => x.IsDefault == false)
                .Where(x => string.IsNullOrEmpty(x.Origin) == false)
                .Select(x => x.Origin!)
                .ToList(),
            PostLogoutRedirectUris = application.PostLogoutRedirectUris,
            RedirectUris = application.RedirectUris,
        };
}