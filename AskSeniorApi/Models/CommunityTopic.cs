using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AskSeniorApi.Models
{
    [Table("communityTopic")]
    public class CommunityTopic : BaseModel
    {
        [PrimaryKey("topic_id", false)]
        [Column("topic_id")]
        public string TopicId { get; set; }

        [PrimaryKey("community_id", false)]
        [Column("community_id")]
        public string CommunityId { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}
