using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AskSeniorApi.Models;

[Table("post")]
public class Post: BaseModel
{
    [PrimaryKey("id", false)]
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

    [Reference(typeof(User), ReferenceAttribute.JoinType.Left)]
    public User User { get; set; }

    [Reference(typeof(Topic), ReferenceAttribute.JoinType.Left)]
    public Topic Topic { get; set; }

    [Reference(typeof(PostImage), ReferenceAttribute.JoinType.Left)]
    public List<PostImage> PostImage { get; set; }
}
