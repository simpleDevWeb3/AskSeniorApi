using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AskSeniorApi.Models;

[Table("vote")]
public class Vote : BaseModel
{
    [PrimaryKey("vote_id")]
    public string voteId { get; set; }

    [Column("post_id")]
    public string PostId { get; set; }

    [Column("user_id")]
    public string UserId { get; set; }

    [Column("is_upvote")]
    public bool IsUpvote { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}