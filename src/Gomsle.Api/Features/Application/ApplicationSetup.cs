using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using AspNetCore.Identity.AmazonDynamoDB;

namespace Gomsle.Api.Features.Application;

public class ApplicationSetup
{
    public const string ApplicationConfigurationsTableName = "application_configuration";

    public static async Task EnsureInitializedAsync(
        IAmazonDynamoDB database,
        CancellationToken cancellationToken = default)
    {
        var applicationConfigurationsGlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
        {
        };

        var tableNames = await database.ListTablesAsync(cancellationToken);

        if (!tableNames.TableNames.Contains(ApplicationConfigurationsTableName))
        {
            await CreateAccountTableAsync(
                database,
                applicationConfigurationsGlobalSecondaryIndexes,
                cancellationToken);
        }
        else
        {
            await DynamoDbUtils.UpdateSecondaryIndexes(
                database,
                ApplicationConfigurationsTableName,
                applicationConfigurationsGlobalSecondaryIndexes,
                cancellationToken);
        }
    }

    private static async Task CreateAccountTableAsync(
        IAmazonDynamoDB database,
        List<GlobalSecondaryIndex>? globalSecondaryIndexes,
        CancellationToken cancellationToken)
    {
        var response = await database.CreateTableAsync(new CreateTableRequest
        {
            TableName = ApplicationConfigurationsTableName,
            BillingMode = BillingMode.PAY_PER_REQUEST,
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement
                {
                    AttributeName = "ApplicationId",
                    KeyType = KeyType.HASH,
                },
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition
                {
                    AttributeName = "ApplicationId",
                    AttributeType = ScalarAttributeType.S,
                },
            },
            GlobalSecondaryIndexes = globalSecondaryIndexes,
        }, cancellationToken);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"Couldn't create table {ApplicationConfigurationsTableName}");
        }

        await DynamoDbUtils.WaitForActiveTableAsync(
            database,
            ApplicationConfigurationsTableName,
            cancellationToken);
    }
}