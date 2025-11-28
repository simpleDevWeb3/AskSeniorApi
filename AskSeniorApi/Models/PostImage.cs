using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AskSeniorApi.Models;

[Table("postImages")]
public class PostImage: BaseModel
{
    [PrimaryKey("image_id", false)]
    [Column("image_id")]
    public string image_id{ get; set; }

    [Column("post_id")]
    public string post_id { get; set; }

    [Column("image_url")]
    public string image_url { get; set; }
}
