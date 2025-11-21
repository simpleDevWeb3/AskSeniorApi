namespace AskSeniorApi.DTO;

public class UserDto
{
    public string? id { get; set; }
    public DateTime created_at { get; set; }
    public string? name { get; set; }
    public string? avatar_url { get; set; }

    public string? banner_url { get; set; }
    public string? bio { get; set; }
}
