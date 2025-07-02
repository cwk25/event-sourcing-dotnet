using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

namespace EventSourcing.Tests.DynamoDb;

public class DynamoDbFixture : IAsyncLifetime
{
    public readonly IAmazonDynamoDB DDbClient = 
        new AmazonDynamoDBClient(
            new BasicAWSCredentials("FakeKey", "FakeSecret"),
            new AmazonDynamoDBConfig
            {
                ServiceURL = "http://localhost:8000"
            });
    public string TableName { get; }= $"test-table-{Guid.NewGuid()}";
    
    public async Task InitializeAsync()
    {
        var createTableRequest = new CreateTableRequest
        {
            TableName = TableName,
            AttributeDefinitions =
            [
                new AttributeDefinition("id", ScalarAttributeType.S),
                new AttributeDefinition("version", ScalarAttributeType.N)
            ],
            KeySchema = [
                new KeySchemaElement
                {
                    AttributeName = "id",
                    KeyType = KeyType.HASH
                },
                new KeySchemaElement
                {
                    AttributeName = "version",
                    KeyType = KeyType.RANGE
                }
            ],
            BillingMode = BillingMode.PAY_PER_REQUEST
        };
        
        await DDbClient.CreateTableAsync(createTableRequest);
        await WaitForTableToBeActive(TableName);
    }

    public Task DisposeAsync()
    {
        return DDbClient.DeleteTableAsync(TableName);
    }
    
    private async Task WaitForTableToBeActive(string tableName)
    {
        string status;
        do
        {
            await Task.Delay(500);
            var response = await DDbClient.DescribeTableAsync(tableName);
            status = response.Table.TableStatus;
        } while (status != "ACTIVE");
    }

}