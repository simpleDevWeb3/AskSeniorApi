using Supabase.Postgrest.Attributes;

namespace AskSeniorApi.DTO;

public class PostInsertDto
{
    public string id { get; set; }
    public string op_id { get; set; }
    public string topic_id { get; set; }
    public string? community_id { get; set; }
    public DateTime created_at { get; set; }
    public string title { get; set; }
    public string text { get; set; }

}
