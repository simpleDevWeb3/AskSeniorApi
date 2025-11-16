namespace AskSeniorApi.DTO;

public class MemberDto
{
    public string user_id { get; set; }
    public string community_id { get; set; }
    public DateTime created_at { get; set; }
    public string? status { get; set; }

}
