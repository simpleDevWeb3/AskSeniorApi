using System.Diagnostics.Contracts;

namespace AskSeniorApi.DTO;
public class CommentCreateDto
{
    public string PostId { get; set; }
    public string UserId { get; set; }

    public string? ParentId { get; set; } // null = top-level, commentId for reply
    public string Content { get; set; }
}
