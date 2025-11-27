namespace AskSeniorApi.DTO;

public class PostCreate
{
    public string? id { get; set; }
    public string user_id { get; set; }
    public string topic_id { get; set; }
    public string? community_id { get; set; }
    public DateTime? created_at { get; set; }
    public string title{ get; set; }
    public string text { get; set; }
}
