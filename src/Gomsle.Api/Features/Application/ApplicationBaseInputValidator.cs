using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using FluentValidation;
using Gomsle.Api.Features.Application.Oidc;
using Gomsle.Api.Infrastructure.Validators;

namespace Gomsle.Api.Features.Application;

public class ApplicationBaseInputValidator : AbstractValidator<ApplicationBaseInput>
{
    public ApplicationBaseInputValidator(IServiceProvider services)
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty();

        RuleFor(x => x.AutoProvision)
            .NotEmpty();

        RuleFor(x => x.EnableProvision)
            .NotEmpty();

        RuleForEach(x => x.RedirectUris)
            .IsUri();

        RuleForEach(x => x.PostLogoutRedirectUris)
            .IsUri();

        RuleFor(x => x.DefaultOrigin)
            .IsUri()
            .When(x => string.IsNullOrEmpty(x.DefaultOrigin) == false);

        When(x => x.DefaultOrigin == default, () =>
        {
            RuleFor(x => x.Origins)
                .Empty();
        });

        When(x => x.Origins?.Any() == true, () =>
        {
            RuleForEach(x => x.Origins)
                .IsUri();

            RuleFor(x => x.Origins)
                .Must((command, origins) => origins.Contains(command.DefaultOrigin!) == false)
                .WithErrorCode(nameof(ErrorCodes.DuplicateOrigin))
                .WithMessage(ErrorCodes.DuplicateOrigin);
        });

        When(x => x.ConnectedOidcProviders?.Any() == true, () =>
        {
            RuleFor(x => x.ConnectedOidcProviders)
                .MustAsync(async (command, providerIds, cancellationToken) =>
                {
                    var database = services.GetRequiredService<IAmazonDynamoDB>();
                    var context = new DynamoDBContext(database);

                    string? accountId = default;

                    if (command.GetType() == typeof(CreateCommand.Command))
                    {
                        accountId = ((CreateCommand.Command)command).AccountId;
                    }
                    else if (command.GetType() == typeof(EditCommand.Command))
                    {
                        var configuration = await context.LoadAsync<ApplicationConfigurationModel>(
                            ((EditCommand.Command)command).Id, cancellationToken);
                        accountId = configuration?.AccountId;
                    }

                    if (accountId == default)
                    {
                        return false;
                    }

                    var search = context.FromQueryAsync<OidcProviderModel>(new QueryOperationConfig
                    {
                        IndexName = "AccountId-index",
                        KeyExpression = new Expression
                        {
                            ExpressionStatement = "AccountId = :accountId",
                            ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                            {
                                { ":accountId", accountId },
                            },
                        },
                    });
                    var providers = await search.GetRemainingAsync(cancellationToken);

                    return providerIds.Any(x => providers.Any(y => y.Id == x) == false) == false;
                })
                .WithErrorCode(nameof(ErrorCodes.InvalidOidcProvider))
                .WithMessage(ErrorCodes.InvalidOidcProvider);
        });
    }
}