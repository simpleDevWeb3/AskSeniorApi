using AskSeniorApi.DTOs;
using AskSeniorApi.Helper;
using AskSeniorApi.Models;
using Microsoft.AspNetCore.Mvc;

using Supabase;

namespace AskSeniorApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommunityController : ControllerBase
{
    private readonly Supabase.Client _client;

    public CommunityController(Supabase.Client client)
    {
        _client = client;
    }

    [HttpPost("create")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateCommunity([FromForm] CreateCommunityRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // 1. Validate Admin Exists
            var adminCheck = await _client
                .From<User>()
                .Select("*")
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, req.AdminId.ToString())
                .Single();

            if (adminCheck == null)
                return BadRequest(new { error = "AdminId does not exist in users table." });

            // 2. Auto-generate Community ID using UNIX format
            long unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string communityId = "C" + unix;

            // 3. Upload files
            string? bannerUrl = null;
            string? avatarUrl = null;

            if (req.BannerFile != null)
                bannerUrl = await UploadFile.UploadFileAsync(req.BannerFile, "Banner", _client);

            if (req.AvatarFile != null)
                avatarUrl = await UploadFile.UploadFileAsync(req.AvatarFile, "Avatar", _client);

            // 4. Insert Community
            var newCommunity = new Community
            {
                Id = communityId,
                AdminId = req.AdminId,
                Name = req.Name,
                Description = req.Description,
                BannerUrl = bannerUrl,
                AvatarUrl = avatarUrl,
                CreatedAt = DateTime.UtcNow
            };

            var insertRes = await _client.From<Community>().Insert(newCommunity);
            var created = insertRes.Models.FirstOrDefault();

            if (created == null)
                return BadRequest(new { error = "Failed to create community." });

            // 5. Insert related CommunityTopic rows
            if (req.TopicIds != null && req.TopicIds.Any())
            {
                // Remove duplicates
                var uniqueTopicIds = req.TopicIds.Distinct();

                foreach (var topicId in uniqueTopicIds)
                {
                    var ct = new CommunityTopic
                    {
                        TopicId = topicId,
                        CommunityId = communityId,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _client.From<CommunityTopic>().Insert(ct);
                }
            }



            return Ok(new
            {
                message = "Community successfully created.",
                communityId = communityId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }








    // READ BY ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCommunity(string id)
    {
        var community = await _client.From<Community>()
            .Where(c => c.Id == id)
            .Single();

        if (community == null)
            return NotFound();

        return Ok(new CommunityDto
        {
            Id = community.Id,
            AdminId = community.AdminId,
            Name = community.Name,
            Description = community.Description,
            BannerUrl = community.BannerUrl,
            AvatarUrl = community.AvatarUrl,
            CreatedAt = community.CreatedAt
        });
    }

    // UPDATE
    [HttpPut("{id}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateCommunity(
    string id,
    [FromForm] UpdateCommunityDto dto,
    [FromHeader] Guid currentUserId)
    {
        try
        {
            var existing = await _client.From<Community>()
                .Where(c => c.Id == id)
                .Single();

            if (existing == null)
                return NotFound(new { error = "Community not found." });

            if (existing.AdminId != currentUserId)
                return Unauthorized(new { error = "Only the community admin can edit this community." });

            // Update text fields
            if (!string.IsNullOrEmpty(dto.Name)) existing.Name = dto.Name;
            if (!string.IsNullOrEmpty(dto.Description)) existing.Description = dto.Description;

            // Update files if provided
            if (dto.BannerFile != null)
                existing.BannerUrl = await UploadFile.UploadFileAsync(dto.BannerFile, "Banner", _client);

            if (dto.AvatarFile != null)
                existing.AvatarUrl = await UploadFile.UploadFileAsync(dto.AvatarFile, "Avatar", _client);

            var result = await _client.From<Community>().Update(existing);
            var updated = result.Models.FirstOrDefault();

            if (updated == null)
                return BadRequest(new { error = "Failed to update community." });

            return Ok(new CommunityDto
            {
                Id = updated.Id,
                AdminId = updated.AdminId,
                Name = updated.Name,
                Description = updated.Description,
                BannerUrl = updated.BannerUrl,
                AvatarUrl = updated.AvatarUrl,
                CreatedAt = updated.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }



    [HttpGet("getall")]
    public async Task<IActionResult> GetAllCommunities()
    {
        try
        {
            var communities = await _client.From<Community>().Get();

            var dtoList = new List<CommunityDto>();

            foreach (var c in communities.Models)
            {
                // Get associated topic IDs
                var communityTopics = await _client.From<CommunityTopic>()
                    .Where(ct => ct.CommunityId == c.Id)
                    .Get();

                var topicIds = communityTopics.Models.Select(ct => ct.TopicId).ToList();

                // Get topic details (name) from Topic table
                var topics = new List<TopicDto>();
                if (topicIds.Any())
                {
                    var topicRecords = await _client
                        .From<Topic>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.In, topicIds)
                        .Get();

                    topics = topicRecords.Models.Select(t => new TopicDto
                    {
                        Id = t.Id,
                        Name = t.Name
                    }).ToList();
                }


                dtoList.Add(new CommunityDto
                {
                    Id = c.Id,
                    AdminId = c.AdminId,
                    Name = c.Name,
                    Description = c.Description,
                    BannerUrl = c.BannerUrl,
                    AvatarUrl = c.AvatarUrl,
                    CreatedAt = c.CreatedAt,
                    Topics = topics
                });
            }

            if (!dtoList.Any())
                return Ok(new { message = "No communities found", data = dtoList });

            return Ok(dtoList);
        }
        catch (Exception ex)
        {
            return Ok(new { message = "Cannot connect to Supabase", error = ex.Message });
        }
    }

}
