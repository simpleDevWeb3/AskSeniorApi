using AskSeniorApi.DTO;
using AskSeniorApi.Models;
using Microsoft.IdentityModel.Tokens;
using Supabase;
using static Supabase.Postgrest.Constants;

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

        var comments = await _supabase
            .From<Comment>()
            .Select("*")
            .Where(c => c.PostId == postId)
            .Order(c => c.CreatedAt, Ordering.Descending)
            .Get();

        var commentDto = comments.Models
            .Select(c => new CommentDto
            {
                comment_id = c.CommentId,
                user_id = c.UserId,
                user_name = c.User.name,
                avatar_url = c.User.avatar_url,
                content = c.Content,
                created_at = c.CreatedAt,
                parent_id = c.ParentId,
                reply_to = c.ParentId.IsNullOrEmpty() ? null : c.Parent.User.name
            })
            .ToList();

        return commentDto;
    }
}
