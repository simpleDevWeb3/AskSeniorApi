using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;


namespace AskSeniorApi.Models;
[Table("userTopicPreference")]
public class UserTopicPreference : BaseModel
{
    [Column("topic_id")]
    public string topic_id { get; set; }


    [Column("user_id")]
    public string user_id { get; set; }

    [Column("created_at")]
    public DateTime created_at { get; set; }


}

