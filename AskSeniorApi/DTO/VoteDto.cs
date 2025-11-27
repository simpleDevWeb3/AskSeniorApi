namespace AskSeniorApi.DTO;

public class VoteDto { 
public string? id { get; set; } 
public string? post_id { get; set; } 
public string? user_id { get; set; } 
public bool is_upvote { get; set; } 
public DateTime created_at { get; set; } 
}

