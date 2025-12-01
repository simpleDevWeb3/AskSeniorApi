using AskSeniorApi.DTO;
using AskSeniorApi.Helpers;
using AskSeniorApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Supabase;
using System.Security.Claims;
using static AskSeniorApi.Models.Auth;
using static Supabase.Postgrest.Constants;

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

    // Create or update vote
    [HttpPost("vote")]
    public async Task<IActionResult> Vote([FromBody] VoteDto dto)
    {
        if (dto == null)
            return BadRequest(new { message = "Request body is required" });

        if (string.IsNullOrWhiteSpace(dto.post_id) && string.IsNullOrWhiteSpace(dto.comment_id))
            return BadRequest(new { message = "Either post_id or comment_id is required" });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? dto.user_id;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "User not authenticated" });

        // Find existing vote
        var query = _supabase.From<Vote>().Where(v => v.UserId == userId);

        if (!string.IsNullOrWhiteSpace(dto.post_id))
            query = query.Where(v => v.PostId == dto.post_id);

        if (!string.IsNullOrWhiteSpace(dto.comment_id))
            query = query.Where(v => v.CommentId == dto.comment_id);

        var existing = await query.Get();

        var currentVote = existing.Models.FirstOrDefault();

        if (currentVote == null)
        {
            // Generate next VoteID
            var allVotesResult = await _supabase
                .From<Vote>()
                .Get(); // fetch all existing votes

            var voteIds = allVotesResult.Models
                .Select(v => v.VoteId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList();

            int maxNumber = 0;
            foreach (var id in voteIds)
            {
                if (id.StartsWith("V") && int.TryParse(id.Substring(1), out int num))
                {
                    if (num > maxNumber)
                        maxNumber = num;
                }
            }

            var nextVoteId = $"V{(maxNumber + 1):D3}";
            // Create new vote
            var newVote = new Vote
            {
                VoteId = nextVoteId,
                PostId = dto.post_id,
                CommentId = dto.comment_id,
                UserId = userId,
                IsUpvote = dto.is_upvote,
                CreatedAt = DateTime.UtcNow
            };

            await _supabase.From<Vote>().Insert(newVote);

            return Ok(new { message = "Vote created", vote = ToDto(newVote) });
        }

        // Same vote → remove (toggle off)
        if (currentVote.IsUpvote == dto.is_upvote)
        {
            await _supabase.From<Vote>()
                .Where(v => v.VoteId == currentVote.VoteId)
                .Delete();

            return Ok(new { message = "Vote removed" });
        }

        // Different vote → update
        currentVote.IsUpvote = dto.is_upvote;
        currentVote.CreatedAt = DateTime.UtcNow;

        await _supabase.From<Vote>().Update(currentVote);

        return Ok(new { message = "Vote updated", vote = ToDto(currentVote) });
    }
    [HttpGet("user")]
    public async Task<IActionResult> GetAllVOtes(string? user_id)
    {
        try
        {
            user_id = user_id.Clean();

            var query = _supabase.From<Vote>().Select("*");

            var vote = await _supabase
                .From<Vote>()
                .Where(v => v.UserId == user_id)
                .Get();
            if (!string.IsNullOrWhiteSpace(user_id))
            {
                query = query.Where(v => v.UserId == user_id);
            }
            if (vote.Models.Count <= 0) return Ok(new List<PostResponeDto>()); ;
            var dtoData = vote.Models.Select(v => new VoteResponseDto
            {
                vote_id = v.VoteId,
                post_id = v.PostId,
                comment_id = v.CommentId,
                user_id = v.UserId,
                is_upvote = v.IsUpvote,
            }).ToList();


            return Ok(dtoData);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteVote([FromBody] DeleteVoteDto dto)
    {
        // Validate required fields
        if (dto == null || string.IsNullOrWhiteSpace(dto.user_id) || string.IsNullOrWhiteSpace(dto.post_id))
            return BadRequest(new { message = "user_id and post_id are required" });

        // Search for vote
        var result = await _supabase
            .From<Vote>()
            .Where(v => v.UserId == dto.user_id && v.PostId == dto.post_id)
            .Get();

        var vote = result.Models.FirstOrDefault();
        if (vote == null)
            return NotFound(new { message = "Vote not found" });

        // Perform deletion
        await _supabase
            .From<Vote>()
            .Where(v => v.VoteId == vote.VoteId)
            .Delete();

        return Ok(new { message = "Vote deleted" });
    }


    // Helper: Convert Vote → DTO
    private VoteDto ToDto(Vote v)
    {
        return new VoteDto
        {
            vote_id = v.VoteId,
            post_id = v.PostId,
            comment_id = v.CommentId,
            user_id = v.UserId,
            is_upvote = v.IsUpvote,
            created_at = v.CreatedAt
        };
    }


}

