using System.Diagnostics.Contracts;

namespace AskSeniorApi.DTO;

public class CommentDto
{
    public string comment_id { get; set; }
    public string user_id { get; set; }
    public string user_name { get; set; }
    public string avatar_url { get; set; }
    public string content { get; set; }
    public DateTime created_at { get; set; }
    public string? parent_id { get; set; }

    public List<CommentDto> sub_comment { get; set; } = new List<CommentDto>();
}


