using AskSeniorApi.DTO;
using AskSeniorApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using System.Security.Claims;
using static AskSeniorApi.Models.Auth;

namespace AskSeniorApi.Controllers;
[Route("api/[controller]")]
[ApiController]
public class VoteController : ControllerBase
{
    private readonly Client _supabase;

    public VoteController(Client supabase)
    {
        _supabase = supabase;
    }

    [HttpPost("vote")]
    public async Task<IActionResult> Vote([FromForm] VoteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.post_id))
            return BadRequest(new { message = "post_id is required" });

        // Get authenticated user ID
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? dto.user_id;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "User not authenticated" });

        // Check existing vote
        var existing = await _supabase
            .From<Vote>()
            .Where(v => v.UserId == userId && v.PostId == dto.post_id)
            .Get();

        var currentVote = existing.Models.FirstOrDefault();

        // No existing vote → create
        if (currentVote == null)
        {
            var newVote = new Vote
            {
                voteId = Guid.NewGuid().ToString(),
                PostId = dto.post_id,
                UserId = userId,
                IsUpvote = dto.is_upvote,
                CreatedAt = DateTime.UtcNow
            };

            await _supabase.From<Vote>().Insert(newVote);

            return Ok(new { message = "Vote created", vote = ToDto(newVote) });
        }

        // Same vote type → delete (toggle off)
        if (currentVote.IsUpvote == dto.is_upvote)
        {
            await _supabase.From<Vote>()
                .Where(v => v.voteId == currentVote.voteId)
                .Delete();

            return Ok(new { message = "Vote removed" });
        }

        // Different vote type → update
        currentVote.IsUpvote = dto.is_upvote;
        currentVote.CreatedAt = DateTime.UtcNow;

        await _supabase.From<Vote>().Update(currentVote);

        return Ok(new { message = "Vote updated", vote = ToDto(currentVote) });
    }

    [HttpDelete("delete/{postId}")]
    public async Task<IActionResult> DeleteVote(string postId)
    {
        // Use authenticated user
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "User not authenticated" });

        var result = await _supabase
            .From<Vote>()
            .Where(v => v.PostId == postId && v.UserId == userId)
            .Get();

        var vote = result.Models.FirstOrDefault();

        if (vote == null)
            return NotFound(new { message = "Vote not found" });

        await _supabase.From<Vote>()
            .Where(v => v.voteId == vote.voteId)
            .Delete();

        return Ok(new { message = "Vote deleted" });
    }

    // Helper: Convert Vote → DTO
    private VoteDto ToDto(Vote v)
    {
        return new VoteDto
        {
            vote_id = v.voteId,
            post_id = v.PostId,
            user_id = v.UserId,
            is_upvote = v.IsUpvote,
            created_at = v.CreatedAt
        };
    }
}

