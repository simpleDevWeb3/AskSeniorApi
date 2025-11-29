using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AskSeniorApi.Models;
using Supabase;
using static AskSeniorApi.Models.Auth;
using AskSeniorApi.DTO;

namespace AskSeniorApi.Controllers;
[Route("api/[controller]")]
[ApiController]
public class CommentController : ControllerBase
{
    private readonly Client _supabase;
    public CommentController(Client supabase)
    {
        _supabase = supabase;
    }
    [HttpGet("post/{postId}")]
    public async Task<IActionResult> GetCommentsByPost(string postId)
    {
        if (string.IsNullOrEmpty(postId))
            return BadRequest(new { message = "postId required" });

        var commentsResult = await _supabase
            .From<Comment>()
            .Select("*")
            .Where(c => c.PostId == postId)
            .Get();

        // Order in C# using LINQ
        var comments = commentsResult.Models
            .OrderBy(c => c.CreatedAt)
            .Select(c => new
            {
                c.CommentId,
                c.Content,
                c.CreatedAt,
                userId = c.UserId,
                parentId =c.ParentId
            })
            .ToList();

        return Ok(comments);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateComment([FromBody] CommentCreateDto dto)
    {
        if (string.IsNullOrEmpty(dto.PostId) || string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest(new { message = "postId and content are required" });

        // 1️⃣ Count existing comments
        var allComments = await _supabase.From<Comment>().Select("comment_id").Get();
        int count = allComments.Models.Count;

        // 2️⃣ Generate comment ID
        string commentId = "C" + (count + 1).ToString("D4"); // e.g., "C0001", "C0002", ...

        // 3️⃣ Create the comment
        var newComment = new Comment
        {
            CommentId = commentId,
            UserId = dto.UserId,
            PostId = dto.PostId,
            ParentId = dto.ParentId, // optional
            Content = dto.Content,
            CreatedAt = DateTime.Now
        };

        await _supabase.From<Comment>().Insert(newComment);

        return Ok(new { message = "Comment created", comment_id = commentId });
    }


    // ===========================
    // EDIT COMMENT
    // ===========================
    [HttpPut("edit/{commentId}")]
    public async Task<IActionResult> EditComment(string commentId, [FromBody] CommentEditDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.user_id) || string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest(new { message = "userId and content are required" });

        // 1️⃣ Get the comment
        var existingComments = await _supabase
            .From<Comment>()
            .Where(c => c.CommentId == commentId)
            .Get();

        if (existingComments.Models.Count == 0)
            return NotFound(new { message = "Comment not found" });

        var comment = existingComments.Models.First();

        // 2️⃣ Check if userId matches
        if (comment.UserId != dto.user_id)
            return Forbid("You can only edit your own comment");

        // 3️⃣ Update fields
        comment.Content = dto.Content;
        comment.ParentId = dto.parent_id; // optional update
                                         // comment.CreatedAt stays the same (or use UpdatedAt if you add that field)

        // 4️⃣ Save to Supabase
        await _supabase.From<Comment>()
            .Where(c => c.CommentId == commentId)
            .Update(comment);

        return Ok(new { message = "Comment updated", comment_id = commentId });
    }

    // ===========================
    // DELETE COMMENT
    // ===========================
    [HttpDelete("delete/{commentId}")]
    public async Task<IActionResult> DeleteComment(string commentId, [FromBody] CommentDeleteDto dto)
    {
        if(string.IsNullOrWhiteSpace(dto.user_id))
            return BadRequest(new { message = "userId is required" });

        var existingResult = await _supabase
            .From<Comment>()
            .Where(c => c.CommentId == commentId)
            .Get();

        if (!existingResult.Models.Any())
            return NotFound(new { message = "Comment not found" });

        var comment = existingResult.Models.First();

        await _supabase.From<Comment>()
            .Where(c => c.CommentId == commentId)
            .Delete();

        return Ok(new { message = "Comment deleted" });
    }
}



