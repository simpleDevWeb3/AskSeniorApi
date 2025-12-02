using System.Diagnostics.Contracts;

namespace AskSeniorApi.DTO;

public class CommentDto
{
    public string post_id { get; set; }
    public string comment_id { get; set; }
    public string user_id { get; set; }
    public string user_name { get; set; }
    public string avatar_url { get; set; }
    public string content { get; set; }
    public DateTime created_at { get; set; }

    public string? parent_id { get; set; }
    public string? reply_to_id { get; set; }
    public string? reply_to_username { get; set; }
    public string? reply_to_content { get; set; }

    public int total_upVote { get; set; } = 0;
    public int total_downVote { get; set; } = 0;
    public bool? self_vote { get; set; }

}


