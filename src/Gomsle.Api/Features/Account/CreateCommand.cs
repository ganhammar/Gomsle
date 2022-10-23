using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using FluentValidation;
using Gomsle.Api.Infrastructure;
using MediatR;

namespace Gomsle.Api.Features.Account;

public class CreateCommand
{
    public class Command : IRequest<IResponse<AccountModel>>
    {
        public string? Name { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator(IAmazonDynamoDB database)
        {
            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MustAsync(async (name, cancellationToken) =>
                {
                    var context = new DynamoDBContext(database);
                    var account = await context.LoadAsync<AccountModel>(
                        name!.UrlFriendly(), cancellationToken);

                    return account == default;
                })
                .WithErrorCode("NameNotUnique")
                .WithMessage("The name is already taken");
        }
    }

    public class CommandHandler : Handler<Command, IResponse<AccountModel>>
    {
        private readonly IAmazonDynamoDB _database;

        public CommandHandler(IAmazonDynamoDB database)
        {
            _database = database;
        }

        public override async Task<IResponse<AccountModel>> Handle(
            Command request, CancellationToken cancellationToken)
        {
            var context = new DynamoDBContext(_database);
            var account = new AccountModel
            {
                Name = request.Name,
                NormalizedName = request.Name!.UrlFriendly(),
            };
            await context.SaveAsync(account);

            return Response(account);
        }
    }
}