using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
namespace AskSeniorApi.Models;

[Table("comments")]

public class Comment : BaseModel
{
    [PrimaryKey("id")]
    public string id { get; set; }
    [Column("post_id")]
    public string post_id { get; set; }
    [Column("user_id")]
    public string user_id { get; set; }
    [Column("content")]
    public string content { get; set; }
    [Column("created_at")]
    public DateTime created_at { get; set; }
}


