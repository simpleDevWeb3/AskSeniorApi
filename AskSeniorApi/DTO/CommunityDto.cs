using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AskSeniorApi.DTOs
{
    public class CreateCommunityRequestJson
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "AdminId is required")]
        public Guid AdminId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        public string? BannerUrl { get; set; }   // string URL instead of IFormFile
        public string? AvatarUrl { get; set; }   // string URL instead of IFormFile
    }
    public class CreateCommunityRequest
    {
        [Required(ErrorMessage = "AdminId is required")]
        public Guid AdminId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [FromForm(Name = "bannerFile")]
        public IFormFile? BannerFile { get; set; }

        [FromForm(Name = "avatarFile")]
        public IFormFile? AvatarFile { get; set; }

        public List<string>? TopicIds { get; set; }
    }

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

        // Optional file uploads for updating images
        public IFormFile? BannerFile { get; set; }
        public IFormFile? AvatarFile { get; set; }
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

        // New property for topics
        public List<TopicDto> Topics { get; set; } = new List<TopicDto>();
    }

    public class TopicDto
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
    }

}
