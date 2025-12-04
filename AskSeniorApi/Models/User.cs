using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AskSeniorApi.Models;

[Table("user")]
public class User : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public string id { get; set; }

    [Column("created_at")]
    public DateTime created_at { get; set; }

    [Column("name")]
    public string name { get; set; }

    [Column("avatar_url")]
    public string? avatar_url { get; set; }


    [Column("banner_url")]
    public string? banner_url { get; set; }

    [Column("email")]
    public string? email { get; set; }

    [Column("bio")]
    public string? bio { get; set; }

    [Column("is_banned")]
    public bool? is_banned { get; set; }
    [Column("role")]
    public string role { get; set; }
}
