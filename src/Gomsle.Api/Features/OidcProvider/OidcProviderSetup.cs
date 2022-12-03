using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using AspNetCore.Identity.AmazonDynamoDB;

namespace Gomsle.Api.Features.OidcProvider;

public class OidcProviderSetup
{
    public const string OidcProvidersTableName = "oidc_providers";
    public const string OidcProviderRequiredDomainsTableName = "oidc_provider_required_domains";

    public static async Task EnsureInitializedAsync(
        IAmazonDynamoDB database,
        CancellationToken cancellationToken = default)
    {
        var oidcProvidersGlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
        {
            new GlobalSecondaryIndex
            {
                IndexName = "AccountId-index",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("AccountId", KeyType.HASH),
                },
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL,
                },
            },
        };
        var oidcProviderRequiredDomainsGlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
        {
            new GlobalSecondaryIndex
            {
                IndexName = "OidcProviderId-index",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("OidcProviderId", KeyType.HASH),
                },
                Projection = new Projection
                {
                    ProjectionType = ProjectionType.ALL,
                },
            },
        };

        var tableNames = await database.ListTablesAsync(cancellationToken);

        if (!tableNames.TableNames.Contains(OidcProvidersTableName))
        {
            await CreateOidcProvidersTableAsync(
                database,
                oidcProvidersGlobalSecondaryIndexes,
                cancellationToken);
        }
        else
        {
            await DynamoDbUtils.UpdateSecondaryIndexes(
                database,
                OidcProvidersTableName,
                oidcProvidersGlobalSecondaryIndexes,
                cancellationToken);
        }

        if (!tableNames.TableNames.Contains(OidcProviderRequiredDomainsTableName))
        {
            await CreateOidcProviderRequiredDomainsTableAsync(
                database,
                oidcProviderRequiredDomainsGlobalSecondaryIndexes,
                cancellationToken);
        }
        else
        {
            await DynamoDbUtils.UpdateSecondaryIndexes(
                database,
                OidcProviderRequiredDomainsTableName,
                oidcProviderRequiredDomainsGlobalSecondaryIndexes,
                cancellationToken);
        }
    }

    private static async Task CreateOidcProvidersTableAsync(
        IAmazonDynamoDB database,
        List<GlobalSecondaryIndex>? globalSecondaryIndexes,
        CancellationToken cancellationToken)
    {
        var response = await database.CreateTableAsync(new CreateTableRequest
        {
            TableName = OidcProvidersTableName,
            BillingMode = BillingMode.PAY_PER_REQUEST,
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement
                {
                    AttributeName = "Id",
                    KeyType = KeyType.HASH,
                },
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition
                {
                    AttributeName = "Id",
                    AttributeType = ScalarAttributeType.S,
                },
                new AttributeDefinition
                {
                    AttributeName = "AccountId",
                    AttributeType = ScalarAttributeType.S,
                },
            },
            GlobalSecondaryIndexes = globalSecondaryIndexes,
        }, cancellationToken);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"Couldn't create table {OidcProvidersTableName}");
        }

        await DynamoDbUtils.WaitForActiveTableAsync(
            database,
            OidcProvidersTableName,
            cancellationToken);
    }

    private static async Task CreateOidcProviderRequiredDomainsTableAsync(
        IAmazonDynamoDB database,
        List<GlobalSecondaryIndex>? globalSecondaryIndexes,
        CancellationToken cancellationToken)
    {
        var response = await database.CreateTableAsync(new CreateTableRequest
        {
            TableName = OidcProviderRequiredDomainsTableName,
            BillingMode = BillingMode.PAY_PER_REQUEST,
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement
                {
                    AttributeName = "RequiredDomain",
                    KeyType = KeyType.HASH,
                },
                new KeySchemaElement
                {
                    AttributeName = "OidcProviderId",
                    KeyType = KeyType.RANGE,
                },
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition
                {
                    AttributeName = "RequiredDomain",
                    AttributeType = ScalarAttributeType.S,
                },
                new AttributeDefinition
                {
                    AttributeName = "OidcProviderId",
                    AttributeType = ScalarAttributeType.S,
                },
            },
            GlobalSecondaryIndexes = globalSecondaryIndexes,
        }, cancellationToken);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new Exception($"Couldn't create table {OidcProviderRequiredDomainsTableName}");
        }

        await DynamoDbUtils.WaitForActiveTableAsync(
            database,
            OidcProviderRequiredDomainsTableName,
            cancellationToken);
    }
}