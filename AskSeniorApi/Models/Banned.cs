using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

using Supabase.Postgrest.Attributes;
namespace AskSeniorApi.Models;

[Table("banned")]
public class Banned: BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public string id {  get; set; }
    [Column("post_id")]
    public string? post_id { get; set; }
    [Column("user_id")]
    public string? user_id { get; set; }
    [Column("community_id")]
    public string? community_id { get; set; }
    [Column("created_at")]
    public DateTime created_at { get; set; }
    [Column("reason")]
    public string reason { get; set; }
}
