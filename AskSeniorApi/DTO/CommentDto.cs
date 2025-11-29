using System.Diagnostics.Contracts;

namespace AskSeniorApi.DTO;

public class CommentDto
{
    public string? comment_id { get; set; }
    public string? post_id { get; set; }
    public string? user_id { get; set; }
    public string? content { get; set; }
    public DateTime? created_at { get; set; }
    public string? parent_id { get; set; }
}


