namespace AskSeniorApi.DTO
{
    public class PostResponeDto
    {
        public string id { get; set; }

        public string user_id { get; set; }
        public string user_name{ get; set; }
        public string? avatar_url { get; set; }

        public string topic_id { get; set; }
        public string topic_name { get; set; }

        public string? community_id { get; set; }
        public string? community_name { get; set; }

        public DateTime created_at { get; set; }

        public string title { get; set; }
        public string text { get; set; }
        public List<string>? postImage_url{ get; set; }

        public int total_upVote { get; set; }
        public int total_downVote { get; set; }
        public int total_comment { get; set; }
    }
}
