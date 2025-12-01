using Microsoft.AspNetCore.Mvc;

namespace AskSeniorApi.DTO;
public class VoteResponseDto
{
    public string vote_id { get; set; } 
    public string user_id { get; set; }
    public string? post_id { get; set; }
    public string? comment_id { get; set; }
    public bool is_upvote { get; set; }

}
