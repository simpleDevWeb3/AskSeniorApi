using AskSeniorApi.DTO;
using AskSeniorApi.Models;
using Supabase;

namespace AskSeniorApi.Helper;

public interface ICommentService
{
    Task<List<CommentDto>> GetCommentsAsync(string postId);
}

public class CommentService : ICommentService
{
    private readonly Client _supabase;

    public CommentService(Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<CommentDto>> GetCommentsAsync(string postId)
    {
        
        var commentsResult = await _supabase
            .From<Comment>()
            .Select("*")
            .Where(c => c.PostId == postId)
            .Get();

        var comments = commentsResult.Models
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto
            {
                comment_id = c.CommentId,
                content = c.Content,
                created_at = c.CreatedAt,
                user_id = c.UserId,
                parent_id = c.ParentId
            })
            .ToList();

        return comments;
        
    }
}
