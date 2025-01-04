using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Linq;
using Newtonsoft.Json;

public class RecommendationService : IRecommendationService
{
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(IMessageBroker messageBroker, ILogger<RecommendationService> logger)
    {
        _messageBroker = messageBroker;
        _logger = logger;
    }

    public async Task<IEnumerable<Product>> GetRecommendationsAsync(int userId)
    {
        _logger.LogInformation("Starting GetRecommendationsAsync for UserId: {UserId}", userId);

        var tcs = new TaskCompletionSource<string>();
        string responseTopic = $"GetRecommendationsResponse_{userId}";

        _logger.LogDebug("Subscribing to response topic: {ResponseTopic}", responseTopic);
        _messageBroker.Subscribe(responseTopic, message =>
        {
            _logger.LogDebug("Received message on topic {ResponseTopic}: {Message}", responseTopic, message);
            tcs.SetResult(message);
        });

        string requestPayload = JsonConvert.SerializeObject(userId);
        _logger.LogDebug("Publishing request to topic 'GetRecommendations' with payload: {Payload}", requestPayload);
        _messageBroker.Publish("GetRecommendations", requestPayload);

        string response;

        try
        {
            response = await tcs.Task;
            _logger.LogDebug("Received response: {Response}", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while waiting for response in GetRecommendationsAsync for UserId: {UserId}", userId);
            throw;
        }

        try
        {
            var recommendations = JsonConvert.DeserializeObject<IEnumerable<Product>>(response)!;
            _logger.LogInformation("Successfully deserialized recommendations for UserId: {UserId}", userId);
            return recommendations;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing recommendations for UserId: {UserId}. Response: {Response}", userId, response);
            throw;
        }
    }
}
