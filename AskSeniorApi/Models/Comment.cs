using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
namespace AskSeniorApi.Models;

[Table("comment")]

public class Comment : BaseModel
{
    [PrimaryKey("comment_id")]
    [Column("comment_id")]
    public string CommentId { get; set; }
    [Column("post_id")]
    public string PostId { get; set; }
    [Column("user_id")]
    public string UserId { get; set; }
    [Column("content")]
    public string Content { get; set; }
    [Column("parent_id")]
    public string ParentId { get; set; }
    [Column("created_at")]

    public DateTime CreatedAt { get; set; }

    [Reference(typeof(User), ReferenceAttribute.JoinType.Left)]
    public User User { get; set; }

    [Reference(typeof(Post), ReferenceAttribute.JoinType.Left)]
    public Post Post { get; set; }
}


