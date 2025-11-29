namespace AskSeniorApi.DTOs
{
    public class CreateCommunityDto
    {
        public string Id { get; set; } = string.Empty;
        public Guid? AdminId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? BannerUrl { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class UpdateCommunityDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? BannerUrl { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class CommunityDto
    {
        public string Id { get; set; } = string.Empty;
        public Guid? AdminId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? BannerUrl { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
