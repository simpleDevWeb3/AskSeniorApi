using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AskSeniorApi.Models;

[Table("topic")]
public class Topic : BaseModel
{
    [PrimaryKey("id")]
    public string? id { get; set; }

    [Column("created_at")]
    public DateTime created_at { get; set; }

    [Column("name")]
    public string? name { get; set; }

    [Column("parent_id")]
    public string? parent_id { get; set; }

  
}
