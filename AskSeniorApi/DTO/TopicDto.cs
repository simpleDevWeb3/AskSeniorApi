namespace AskSeniorApi.DTO;

public class TopicDto
{
    public string id { get; set; }
    public string name { get; set; }
    public string? parent_id { get; set; }
    public DateTime created_at { get; set; }

    public List<TopicDto> sub_topic { get; set; } = new List<TopicDto>();
}
