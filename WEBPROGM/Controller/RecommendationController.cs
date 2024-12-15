using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class RecommendationController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;

    public RecommendationController(IRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetRecommendations(int userId)
    {
        var recommendations = await _recommendationService.GetRecommendationsAsync(userId);
        return Ok(recommendations);
    }
}