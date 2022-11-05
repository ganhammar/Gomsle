using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using FluentValidation;
using Gomsle.Api.Infrastructure;
using Gomsle.Api.Infrastructure.Extensions;
using MediatR;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Gomsle.Api.Features.Account;

public class CreateCommand
{
    public class Command : IRequest<IResponse<AccountModel>>
    {
        public string? Name { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator(
            IAmazonDynamoDB database,
            IHttpContextAccessor httpContextAccessor)
        {
            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MustAsync(async (name, cancellationToken) =>
                {
                    var context = new DynamoDBContext(database);
                    var search = context.FromQueryAsync<AccountModel>(new QueryOperationConfig
                    {
                        IndexName = "NormalizedName-index",
                        KeyExpression = new Expression
                        {
                            ExpressionStatement = "NormalizedName = :normalizedName",
                            ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                            {
                                { ":normalizedName", name!.UrlFriendly() },
                            }
                        },
                        Limit = 1,
                    });
                    var accounts = await search.GetRemainingAsync(cancellationToken);

                    return accounts.Any() == false;
                })
                .WithErrorCode("NameNotUnique")
                .WithMessage("The name is already taken");

            RuleFor(x => x)
                .Must((command) => httpContextAccessor?.HttpContext
                    ?.User?.Identity?.IsAuthenticated == true)
                .WithErrorCode("NotAuthorized")
                .WithMessage("User not authorized");
        }
    }

    public class CommandHandler : Handler<Command, IResponse<AccountModel>>
    {
        private readonly IAmazonDynamoDB _database;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CommandHandler(
            IAmazonDynamoDB database,
            IHttpContextAccessor httpContextAccessor)
        {
            _database = database;
            _httpContextAccessor = httpContextAccessor;
        }

        public override async Task<IResponse<AccountModel>> Handle(
            Command request, CancellationToken cancellationToken)
        {
            var context = new DynamoDBContext(_database);
            var userId = _httpContextAccessor.HttpContext!.User.GetUserId();
            var account = new AccountModel
            {
                Name = request.Name,
                NormalizedName = request.Name!.UrlFriendly(),
                Members = new Dictionary<string, AccountRole>
                {
                    { userId!, AccountRole.Owner },
                },
            };
            await context.SaveAsync(account);

            return Response(account);
        }
    }
}