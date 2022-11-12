using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using FluentValidation;
using Gomsle.Api.Features.Account;
using Gomsle.Api.Infrastructure;
using Gomsle.Api.Infrastructure.Validators;
using MediatR;

namespace Gomsle.Api.Features.Application;

public class DeleteCommand
{
    public class Command : IRequest<IResponse>
    {
        public string? Id { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator(IServiceProvider services)
        {
            RuleFor(x => x.Id)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MustAsync(async (command, id, cancellationToken) =>
                {
                    var dbContext = new DynamoDBContext(
                        services.GetRequiredService<IAmazonDynamoDB>());
                    var configuration = await dbContext.LoadAsync<ApplicationConfigurationModel>(id, cancellationToken);

                    if (configuration == default)
                    {
                        return false;
                    }

                    return await HasRoleForAccount.Validate(
                        services,
                        configuration.AccountId,
                        new[] { AccountRole.Administrator, AccountRole.Owner },
                        cancellationToken);
                })
                .WithErrorCode(nameof(ErrorCodes.NotAuthorized))
                .WithMessage(ErrorCodes.NotAuthorized);
        }
    }

    public class CommandHandler : Handler<Command, IResponse>
    {
        private readonly DynamoDBContext _dbContext;

        public CommandHandler(IAmazonDynamoDB database)
        {
            _dbContext = new DynamoDBContext(database);
        }

        public override async Task<IResponse> Handle(
            Command request, CancellationToken cancellationToken)
        {
            await _dbContext.DeleteAsync<ApplicationConfigurationModel>(request.Id, cancellationToken);

            return Response();
        }
    }
}