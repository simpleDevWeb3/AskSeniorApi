using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AskSeniorApi.Models;

[Table("vote")]
public class Vote : BaseModel
{
    [PrimaryKey("vote_id", false)]
    [Column ("vote_id")]
    public string VoteId { get; set; }

    [Column("post_id")]
    public string PostId { get; set; }
   
    [Column("comment_id")]
    public string? CommentId { get; set; }

    [Column("user_id")]
    public string UserId { get; set; }

    [Column("is_upvote")]
    public bool IsUpvote { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Reference(typeof(User), ReferenceAttribute.JoinType.Left)]
    public User User { get; set; }

    [Reference(typeof(Post), ReferenceAttribute.JoinType.Left)]
    public Post Post { get; set; }

    [Reference(typeof(CommentRespone), ReferenceAttribute.JoinType.Left)]
    public CommentRespone Comment { get; set; }
}