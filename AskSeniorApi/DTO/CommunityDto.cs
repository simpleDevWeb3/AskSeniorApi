using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AskSeniorApi.DTOs
{
    public class CommunityWithJoinAndBanStatusDto
    {
        public string Id { get; set; }
        public Guid AdminId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string BannerUrl { get; set; }
        public string AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<TopicDto> Topics { get; set; } = new();

        public bool IsJoined { get; set; }
        public bool IsBanned { get; set; }

        public int MemberCount { get; set; }   // ✅ NEW
    }


    public class CommunityWithStatsDto
    {
        public string Id { get; set; }
        public Guid AdminId { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public string? BannerUrl { get; set; }
        public string? AvatarUrl { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<TopicDto> Topics { get; set; } = new();

        public bool IsJoined { get; set; }

        // NEW
        public int MemberCount { get; set; }
        public bool IsBanned { get; set; }
    }

    public class BanCommunityRequest
    {
        // the user (requester) performing the ban
        public Guid RequesterUserId { get; set; }

        // the community to ban
        public string CommunityId { get; set; } = string.Empty;

        // optional reason
        public string? Reason { get; set; }
    }

    public class CommunityWithJoinStatusDto
    {
        public string Id { get; set; }
        public Guid AdminId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string BannerUrl { get; set; }
        public string AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<TopicDto> Topics { get; set; } = new();

        public bool IsJoined { get; set; }
    }


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
        [Required(ErrorMessage = "AdminId is required.")]
        public Guid AdminId { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = string.Empty;

        [FromForm(Name = "bannerFile")]
        public IFormFile? BannerFile { get; set; }

        [FromForm(Name = "avatarFile")]
        public IFormFile? AvatarFile { get; set; }

        [Required(ErrorMessage = "TopicIds is required and cannot be empty.")]
        [MinLength(1, ErrorMessage = "You must include at least one topic.")]
        public List<string> TopicIds { get; set; } = new();
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

    public class UpdateCommunityRequest
    {
        [Required]
        public string CommunityId { get; set; } = string.Empty;

        public string? Name { get; set; }
        public string? Description { get; set; }

        [FromForm(Name = "bannerFile")]
        public IFormFile? BannerFile { get; set; }

        [FromForm(Name = "avatarFile")]
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
