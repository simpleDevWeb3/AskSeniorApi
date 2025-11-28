using System.Diagnostics.Contracts;

namespace AskSeniorApi.DTO;

public class PostCreateDto
{
    public string user_id { get; set; }
    public string topic_id { get; set; }
    public string? community_id { get; set; }
    public string title{ get; set; }
    public string text { get; set; }

    public IFormFile[]? image { get; set; }
}
