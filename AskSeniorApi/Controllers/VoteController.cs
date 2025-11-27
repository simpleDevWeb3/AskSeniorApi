using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AskSeniorApi.Models;
using Supabase;
using static AskSeniorApi.Models.Auth;
using AskSeniorApi.DTO;

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

    [HttpPost]
    public async Task<IActionResult> Vote(VoteDto dto)
    {
        // Find existing vote
        var existing = await _supabase
            .From<Vote>()
            .Where(v => v.UserId == dto.user_id && v.PostId == dto.post_id)
            .Get();

        var currentVote = existing.Models.FirstOrDefault();

        // 1️⃣ No existing → create new vote
        if (currentVote == null)
        {
            var newVote = new Vote
            {
                Id = Guid.NewGuid().ToString(),
                PostId = dto.post_id,
                UserId = dto.user_id,
                IsUpvote = dto.is_upvote,
                CreatedAt = DateTime.UtcNow
            };

            await _supabase.From<Vote>().Insert(newVote);

            return Ok(new
            {
                message = "Vote created",
                vote = ToDto(newVote)
            });
        }

        // 2️⃣ Same type → delete vote (toggle off)
        if (currentVote.IsUpvote == dto.is_upvote)
        {
            await _supabase
                .From<Vote>()
                .Where(v => v.Id == currentVote.Id)
                .Delete();

            return Ok(new { message = "Vote removed" });
        }

        // 3️⃣ Different type → update
        currentVote.IsUpvote = dto.is_upvote;
        currentVote.CreatedAt = DateTime.UtcNow;

        await _supabase.From<Vote>().Update(currentVote);

        return Ok(new
        {
            message = "Vote updated",
            vote = ToDto(currentVote)
        });
    }

    [HttpDelete("{postId}/{userId}")]
    public async Task<IActionResult> DeleteVote(string postId, string userId)
    {
        var result = await _supabase
            .From<Vote>()
            .Where(v => v.PostId == postId && v.UserId == userId)
            .Get();

        var vote = result.Models.FirstOrDefault();

        if (vote == null)
            return NotFound(new { message = "Vote not found" });

        await _supabase.From<Vote>().Where(v => v.Id == vote.Id).Delete();

        return Ok(new { message = "Vote deleted" });
    }


    // Helper: Convert Model → DTO
    private VoteDto ToDto(Vote v)
    {
        return new VoteDto
        {
            id = v.Id,
            post_id = v.PostId,
            user_id = v.UserId,
            is_upvote = v.IsUpvote,
            created_at = v.CreatedAt
        };
    }
}

