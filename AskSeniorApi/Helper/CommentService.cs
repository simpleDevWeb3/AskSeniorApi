using AskSeniorApi.DTO;
using AskSeniorApi.Models;
using Supabase;

namespace AskSeniorApi.Helper;

public interface ICommentService
{
    Task<List<CommentDto>> GetCommentsAsync(string postId);
    //Task<List<CommentDto>> GetCommentsAsync2(string postId);
}

public class CommentService : ICommentService
{
    private readonly Client _supabase;

    public CommentService(Client supabase)
    {
        _supabase = supabase;
    }
    /*
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
                user_id = c.UserId,
                content = c.Content,
                created_at = c.CreatedAt,
                parent_id = c.ParentId
            })
            .ToList();

        return comments;
        
    }
    */
    public async Task<List<CommentDto>> GetCommentsAsync(string postId)
    {

        var comments = await _supabase
            .From<Comment>()
            .Select("*")
            .Where(c => c.PostId == postId)
            .Get();

        var commentDto = comments.Models
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto
            {
                comment_id = c.CommentId,
                user_id = c.UserId,
                user_name = c.User.name,
                avatar_url = c.User.avatar_url,
                content = c.Content,
                created_at = c.CreatedAt,
                parent_id = c.ParentId
            })
            .ToList();

        /*
        var result = BuildSubHelper.BuildHierarchy<CommentDto>(
            commentDto,
            getParentId: c => c.parent_id,
            getId: c => c.comment_id!,
            setChildren: (parent, children) => parent.sub_comment = children
        );
        */

        return commentDto;
    }
}
