using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AskSeniorApi.Models;

[Table("member")]
public class Member:BaseModel
{
    [Column("user_id")]
    public string user_id { get; set; }

    [Column("created_at")]
    public DateTime created_at { get; set; }

    [Column("community_id")]
    public string community_id { get; set; }

    [Column("status")]
    public string? status { get; set; }
}
