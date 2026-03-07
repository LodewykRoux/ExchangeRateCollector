using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ExchangeRateCollector.ExchangeRate.Interface;
using Microsoft.Extensions.Logging;

namespace ExchangeRateCollector.ExchangeRate;

public class ExchangeRatesApi : IExchangeRatesApi
{
    private readonly HttpClient _client = new();
    private readonly ILogger<ExchangeRatesApi> _logger;
    private readonly string _exchangeRateApiUrl = Environment.GetEnvironmentVariable("ExchangeRateApiUrl") ?? string.Empty;
    private readonly string _exchangeRateApiAccessKey = Environment.GetEnvironmentVariable("ExchangeRateApiAccessKey") ?? string.Empty;
    private readonly string _exchangeRateApiBaseCurrency = Environment.GetEnvironmentVariable("ExchangeRateApiBaseCurrency") ?? string.Empty;

    public ExchangeRatesApi(ILogger<ExchangeRatesApi> logger)
    {
        _logger = logger;
    }
    
    public async Task<Dictionary<string, decimal>> FetchRatesAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_exchangeRateApiUrl) ||
            string.IsNullOrWhiteSpace(_exchangeRateApiAccessKey) ||
            string.IsNullOrWhiteSpace(_exchangeRateApiBaseCurrency))
        {
            _logger.LogError(
                "Exchange rate API configuration is invalid. Url configured: {HasUrl}, AccessKey configured: {HasAccessKey}, BaseCurrency configured: {HasBaseCurrency}",
                !string.IsNullOrWhiteSpace(_exchangeRateApiUrl),
                !string.IsNullOrWhiteSpace(_exchangeRateApiAccessKey),
                !string.IsNullOrWhiteSpace(_exchangeRateApiBaseCurrency));

            return [];
        }

        var baseCurrency = _exchangeRateApiBaseCurrency.Trim().ToUpperInvariant();

        var requestUri =
            $"{_exchangeRateApiUrl.TrimEnd('/')}/{Uri.EscapeDataString(_exchangeRateApiAccessKey)}/latest/{Uri.EscapeDataString(baseCurrency)}";

        try
        {
            _logger.LogInformation(
                "Fetching exchange rates for base currency {BaseCurrency}",
                baseCurrency);

            using var response = await _client.GetAsync(requestUri, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Exchange rate API request failed. StatusCode: {StatusCode}. Response: {Response}",
                    response.StatusCode,
                    responseBody);

                return [];
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ExchangeRatesApiResponse>(cancellationToken: cancellationToken);

            if (apiResponse is null)
            {
                _logger.LogError("Exchange rate API returned an empty response body.");
                return [];
            }

            if (!string.Equals(apiResponse.Result, "success", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "Exchange rate API returned unsuccessful response. Result: {Result}, ErrorType: {ErrorType}",
                    apiResponse.Result,
                    apiResponse.ErrorType);

                return [];
            }

            if (apiResponse.ConversionRates.Count == 0)
            {
                _logger.LogWarning("Exchange rate API returned no conversion rates.");
                return [];
            }

            _logger.LogInformation(
                "Successfully fetched {RateCount} exchange rates for base currency {BaseCurrency}",
                apiResponse.ConversionRates.Count,
                apiResponse.BaseCode);

            return apiResponse.ConversionRates;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Exchange rate API request timed out.");
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching exchange rates.");
            return [];
        }
    }

    private sealed class ExchangeRatesApiResponse
    {
        public string Result { get; init; } = string.Empty;

        [JsonPropertyName("documentation")]
        public string Documentation { get; init; } = string.Empty;

        [JsonPropertyName("terms_of_use")]
        public string TermsOfUse { get; init; } = string.Empty;

        [JsonPropertyName("time_last_update_unix")]
        public long TimeLastUpdateUnix { get; init; }

        [JsonPropertyName("time_last_update_utc")]
        public string TimeLastUpdateUtc { get; init; } = string.Empty;

        [JsonPropertyName("time_next_update_unix")]
        public long TimeNextUpdateUnix { get; init; }

        [JsonPropertyName("time_next_update_utc")]
        public string TimeNextUpdateUtc { get; init; } = string.Empty;

        [JsonPropertyName("base_code")]
        public string BaseCode { get; init; } = string.Empty;

        [JsonPropertyName("conversion_rates")]
        public Dictionary<string, decimal> ConversionRates { get; init; } = [];

        [JsonPropertyName("error-type")]
        public string? ErrorType { get; init; }
    }
}