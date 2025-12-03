namespace AskSeniorApi.DTO;

public class UserDto
{
    public string? id { get; set; }
    public DateTime created_at { get; set; }
    public string? name { get; set; }
    public string? avatar_url { get; set; }

    public string? banner_url { get; set; }
    public string? email { get; set; }
    public string? bio { get; set; }
}

public class UserEditDto
{
    public string? name { get; set; }
    public string? Password { get; set; }
    public string? bio { get; set; }

    public IFormFile? AvatarFile { get; set; }
    public IFormFile? BannerFile { get; set; }

    public List<string>? Preference { get; set; }
}
