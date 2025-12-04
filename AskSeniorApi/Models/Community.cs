using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AskSeniorApi.Models;

[Table("community")]
public class Community : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("admin_Id")]   // must match EXACT db column
    public Guid? AdminId { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("banner_url")]
    public string? BannerUrl { get; set; }

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("is_banned")]
    public bool IsBanned { get; set; } = false;   // default = false
}
