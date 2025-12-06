using AskSeniorApi.DTO;
using AskSeniorApi.Models;
using Microsoft.IdentityModel.Tokens;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace AskSeniorApi.Helper;

public interface ICommentService
{
    Task<List<CommentDto>> GetCommentsAsync(string? postId = null, string? user_id = null, string? current_user = null);
}

public class CommentService : ICommentService
{
    private readonly Client _supabase;

    public CommentService(Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<CommentDto>> GetCommentsAsync(
       string? postId = null,
       string? user_id = null,             
       string? current_user = null)
    {

        var query = _supabase
                    .From<CommentRespone>()
                    .Select("*, vote(*)")
                    .Order(c => c.CreatedAt, Ordering.Descending);

        if (!postId.IsNullOrEmpty())
        {
            query = query.Where(c => c.PostId == postId);
        }

        if (!user_id.IsNullOrEmpty())
        {
            query = query.Where(c => c.UserId == user_id);
        }

        var comments = await query.Get();

        var commentDto = comments.Models
            .Select(c => new CommentDto
            {
                post_id = c.PostId,
                comment_id = c.CommentId,
                user_id = c.UserId,
                user_name = c.User.name,
                avatar_url = c.User.avatar_url,
                content = c.Content,
                created_at = c.CreatedAt,
                parent_id = c.ParentId,
                reply_to_id = c.ParentId.IsNullOrEmpty() ? null : c.Parent.UserId,
                reply_to_username = c.ParentId.IsNullOrEmpty() ? null : c.Parent.User.name,
                reply_to_content = c.ParentId.IsNullOrEmpty() ? null : c.Parent.Content,
                total_upVote = c.vote?.Count(v => v.IsUpvote && v.CommentId == c.CommentId) ?? 0,
                total_downVote = c.vote?.Count(v => !v.IsUpvote && v.CommentId == c.CommentId) ?? 0,
                self_vote = c.vote?
                            .Where(v => v.UserId == current_user)
                            .Select(v => (bool?)v.IsUpvote)   // cast to nullable bool
                            .FirstOrDefault(),
            })
            .ToList();

        return commentDto;
    }
}
