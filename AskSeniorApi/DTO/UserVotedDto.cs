namespace AskSeniorApi.DTO;

public class UserVotedDto
{
    //post or comment
    public string? type { get; set; }
    public string? post_id { get; set; } = null;
    public string? comment_id { get; set; } = null;


    //user
    public string user_id { get; set; }
    public string user_name { get; set; }
    public string avatar_url { get; set; }
    
    
    //post
    public string? topic_name { get; set; } = null;
    public string? community_name { get; set; } = null;
    public string? title { get; set; } = null;
    public string? text { get; set; } = null;
    public Dictionary<string, string>? postImage_url { get; set; } = new Dictionary<string, string>();


    //comment
    public string? comment_content { get; set; } = null;
    public string? reply_to_userId { get; set; } = null;
    public string? reply_to_username { get; set; } = null;
    public string? reply_to_content { get; set; } = null;


    //statistic
    public int total_comment {  get; set; } = 0;
    public int total_upVote { get; set; } = 0;
    public int total_downVote { get; set; } = 0;
    public DateTime created_at { get; set; }
    public DateTime vote_created_at { get; set; }
    public bool? self_vote { get; set; }
}
