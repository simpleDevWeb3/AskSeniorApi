using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AskSeniorApi.Models;

[Table("post")]
public class Post: BaseModel
{
    [Column("id")]
    public string id { get; set; }
    [Column("op_id")]
    public string user_id { get; set; }
    [Column("topic_id")]
    public string topic_id { get; set; }
    [Column("community_id")]
    public string? community_id { get; set; }
    [Column("created_at")]
    public DateTime created_at { get; set; }
    [Column("title")]
    public string title { get; set; }
    [Column("text")]
    public string text { get; set; }
}
