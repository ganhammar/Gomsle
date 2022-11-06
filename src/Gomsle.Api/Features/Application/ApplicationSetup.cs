using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using AspNetCore.Identity.AmazonDynamoDB;

namespace Gomsle.Api.Features.Application;

public class ApplicationSetup
{
    public const string ApplicationConfigurationsTableName = "application_configurations";
    public const string ApplicationOriginsTableName = "application_origins";

    public static async Task EnsureInitializedAsync(
        IAmazonDynamoDB database,
        CancellationToken cancellationToken = default)
    {
        var applicationConfigurationsGlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
        {
        };

        var applicationOriginsGlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
        {
        };

        var tableNames = await database.ListTablesAsync(cancellationToken);

        if (!tableNames.TableNames.Contains(ApplicationConfigurationsTableName))
        {
            await CreateApplicationConfigurationsTableAsync(
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

        if (!tableNames.TableNames.Contains(ApplicationOriginsTableName))
        {
            await CreateApplicationOriginsTableAsync(
                database,
                applicationOriginsGlobalSecondaryIndexes,
                cancellationToken);
        }
        else
        {
            await DynamoDbUtils.UpdateSecondaryIndexes(
                database,
                ApplicationOriginsTableName,
                applicationOriginsGlobalSecondaryIndexes,
                cancellationToken);
        }
    }

    private static async Task CreateApplicationConfigurationsTableAsync(
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

    private static async Task CreateApplicationOriginsTableAsync(
        IAmazonDynamoDB database,
        List<GlobalSecondaryIndex>? globalSecondaryIndexes,
        CancellationToken cancellationToken)
    {
        var response = await database.CreateTableAsync(new CreateTableRequest
        {
            TableName = ApplicationOriginsTableName,
            BillingMode = BillingMode.PAY_PER_REQUEST,
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement
                {
                    AttributeName = "Origin",
                    KeyType = KeyType.HASH,
                },
                new KeySchemaElement
                {
                    AttributeName = "ApplicationId",
                    KeyType = KeyType.RANGE,
                },
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition
                {
                    AttributeName = "Origin",
                    AttributeType = ScalarAttributeType.S,
                },
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
            throw new Exception($"Couldn't create table {ApplicationOriginsTableName}");
        }

        await DynamoDbUtils.WaitForActiveTableAsync(
            database,
            ApplicationOriginsTableName,
            cancellationToken);
    }
}