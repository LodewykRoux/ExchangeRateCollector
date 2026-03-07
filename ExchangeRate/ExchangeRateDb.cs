using System.ComponentModel.DataAnnotations;
using ExchangeRateCollector.ExchangeRate.Interface;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ExchangeRateCollector.ExchangeRate;

public class ExchangeRateDb(ILogger<ExchangeRateDb> logger) : IExchangeRateDb
{
    private readonly string _cosmosDbDocumentEndpoint =
        Environment.GetEnvironmentVariable("CosmosDbDocumentEndpoint") ?? string.Empty;

    private readonly string _cosmosDbAccountKey =
        Environment.GetEnvironmentVariable("CosmosDbAccountKey") ?? string.Empty;

    private readonly string _cosmosDbDatabaseName =
        Environment.GetEnvironmentVariable("CosmosDbDatabaseName") ?? string.Empty;

    private readonly string _cosmosDbContainerName =
        Environment.GetEnvironmentVariable("CosmosDbContainerName") ?? string.Empty;

    private readonly string _exchangeRateApiBaseCurrency =
        Environment.GetEnvironmentVariable("ExchangeRateApiBaseCurrency") ?? string.Empty;

    public async Task SaveExchangeRateAsync(Dictionary<string, decimal> exchangeRates)
    {
        if (string.IsNullOrWhiteSpace(_cosmosDbDocumentEndpoint) ||
            string.IsNullOrWhiteSpace(_cosmosDbAccountKey) ||
            string.IsNullOrWhiteSpace(_cosmosDbDatabaseName) ||
            string.IsNullOrWhiteSpace(_cosmosDbContainerName))
        {
            logger.LogError(
                "Exchange rate DB configuration is invalid. Endpoint configured: {HasEndpoint}, Account key configured: {HasAccountKey}, Database name configured: {HasDatabaseName}, Container name configured: {HasContainerName}",
                !string.IsNullOrWhiteSpace(_cosmosDbDocumentEndpoint),
                !string.IsNullOrWhiteSpace(_cosmosDbAccountKey),
                !string.IsNullOrWhiteSpace(_cosmosDbDatabaseName),
                !string.IsNullOrWhiteSpace(_cosmosDbContainerName));

            return;
        }

        if (exchangeRates.Count == 0)
        {
            logger.LogWarning("No exchange rates were provided. Skipping Cosmos DB save.");
            return;
        }

        var client = new CosmosClient(
            accountEndpoint: _cosmosDbDocumentEndpoint,
            authKeyOrResourceToken: _cosmosDbAccountKey
        );

        try
        {
            Database database = await client.CreateDatabaseIfNotExistsAsync(_cosmosDbDatabaseName);

            Container container = await database.CreateContainerIfNotExistsAsync(
                id: _cosmosDbContainerName,
                partitionKeyPath: "/id"
            );

            var exchangeRateDocument = new ExchangeRateDocument
            {
                Id = Guid.NewGuid().ToString(),
                BaseCurrency = _exchangeRateApiBaseCurrency.Trim().ToUpperInvariant(),
                Date = DateTime.UtcNow,
                ExchangeRates = exchangeRates
            };

            await container.CreateItemAsync(exchangeRateDocument, new PartitionKey(exchangeRateDocument.Id));

            logger.LogInformation(
                "Added exchange rate entry at {Timestamp} with {ExchangeRateCount} rates to Cosmos DB.",
                exchangeRateDocument.Date,
                exchangeRates.Count);
        }
        catch (CosmosException ex)
        {
            logger.LogError(
                ex,
                "Cosmos DB error while saving exchange rates. StatusCode: {StatusCode}",
                ex.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while saving exchange rates to Cosmos DB.");
        }
    }

    private class ExchangeRateDocument
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; init; } = string.Empty;

        [StringLength(3), JsonProperty(PropertyName = "baseCurrency")]
        public string BaseCurrency { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "date")]
        public DateTime Date { get; set; }

        [JsonProperty(PropertyName = "exchangeRates")]
        public Dictionary<string, decimal> ExchangeRates { get; set; } = [];
    }
}