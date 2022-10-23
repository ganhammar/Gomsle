using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Gomsle.Api.Infrastructure;

namespace Gomsle.Api.Features.Account;

public static class AccountSetup
{
    public const string TableName = "accounts";

    public static async Task EnsureInitializedAsync(
        IAmazonDynamoDB database,
        CancellationToken cancellationToken = default)
    {
        var globalSecondaryIndexes = new List<GlobalSecondaryIndex>
        {
        };

        var tableNames = await database.ListTablesAsync(cancellationToken);

        if (!tableNames.TableNames.Contains(TableName))
        {
            await CreateTableAsync(
                database,
                globalSecondaryIndexes,
                cancellationToken);
        }
        else
        {
            await DynamoDbUtils.UpdateSecondaryIndexes(
                database,
                TableName,
                globalSecondaryIndexes,
                cancellationToken);
        }
    }

    private static async Task CreateTableAsync(
        IAmazonDynamoDB database,
        List<GlobalSecondaryIndex>? globalSecondaryIndexes,
        CancellationToken cancellationToken)
    {
        var response = await database.CreateTableAsync(new CreateTableRequest
        {
            TableName = TableName,
            BillingMode = BillingMode.PAY_PER_REQUEST,
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement
                {
                    AttributeName = "NormalizedName",
                    KeyType = KeyType.HASH,
                },
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition
                {
                    AttributeName = "NormalizedName",
                    AttributeType = ScalarAttributeType.S,
                },
            },
            GlobalSecondaryIndexes = globalSecondaryIndexes,
        }, cancellationToken);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"Couldn't create table {TableName}");
        }

        await DynamoDbUtils.WaitForActiveTableAsync(
            database,
            TableName,
            cancellationToken);
    }
}