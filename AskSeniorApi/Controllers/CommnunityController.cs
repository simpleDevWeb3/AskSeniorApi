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
            foreach (var topicId in req.TopicIds.Distinct())
            {
                var ct = new CommunityTopic
                {
                    TopicId = topicId,
                    CommunityId = communityId,
                    CreatedAt = DateTime.UtcNow
                };

                await _client.From<CommunityTopic>().Insert(ct);
            }


            // 6. Automatically join creator to the community
            var newMember = new Member
            {
                user_id = req.AdminId.ToString(),
                community_id = communityId,
                created_at = DateTime.UtcNow,
                status = "joined"
            };

            await _client.From<Member>().Insert(newMember);

            return Ok(new
            {
                message = "Community successfully created and creator joined automatically.",
                communityId = communityId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }









    [HttpGet("search")]
    public async Task<IActionResult> SearchCommunities([FromQuery] string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest(new { error = "Keyword is required." });

        try
        {
            // 1. Search for communities using ILIKE (case-insensitive, supports partial)
            var communitiesResponse = await _client
                .From<Community>()
                .Filter("name", Supabase.Postgrest.Constants.Operator.ILike, $"%{keyword}%")
                .Get();

            var communities = communitiesResponse.Models;

            if (!communities.Any())
                return Ok(new { message = "No communities match your search.", data = new List<CommunityDto>() });

            // 2. Fetch all community-topic links
            var linksResponse = await _client.From<CommunityTopic>().Get();
            var links = linksResponse.Models;

            // 3. Collect all topic IDs referenced by these communities
            var topicIds = links
                .Where(l => communities.Any(c => c.Id == l.CommunityId))
                .Select(l => l.TopicId)
                .Distinct()
                .ToList();

            // 4. Fetch topic details in one query
            var topicRecordsResponse = await _client
                .From<Topic>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.In, topicIds)
                .Get();

            var topicMap = topicRecordsResponse.Models.ToDictionary(t => t.id);

            // 5. Build the final DTO output
            var dtoList = communities.Select(c =>
            {
                var relatedTopics = links
                    .Where(l => l.CommunityId == c.Id)
                    .Select(l => l.TopicId)
                    .Where(id => topicMap.ContainsKey(id))
                    .Select(id => new TopicDto
                    {
                        Id = id,
                        Name = topicMap[id].name
                    })
                    .ToList();

                return new CommunityDto
                {
                    Id = c.Id,
                    AdminId = c.AdminId,
                    Name = c.Name,
                    Description = c.Description,
                    BannerUrl = c.BannerUrl,
                    AvatarUrl = c.AvatarUrl,
                    CreatedAt = c.CreatedAt,
                    Topics = relatedTopics
                };
            }).ToList();

            return Ok(dtoList);
        }
        catch (Exception ex)
        {
            return Ok(new { error = ex.Message });
        }
    }


    [HttpPut("update")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateCommunity([FromForm] UpdateCommunityRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // 1. Check if community exists
            var communityRes = await _client
                .From<Community>()
                .Select("*")
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, req.CommunityId)
                .Single();

            var existing = communityRes;

            if (existing == null)
                return NotFound(new { error = "Community not found." });

            // 2. Upload new files if provided
            string? bannerUrl = existing.BannerUrl;
            string? avatarUrl = existing.AvatarUrl;

            if (req.BannerFile != null)
                bannerUrl = await UploadFile.UploadFileAsync(req.BannerFile, "Banner", _client);

            if (req.AvatarFile != null)
                avatarUrl = await UploadFile.UploadFileAsync(req.AvatarFile, "Avatar", _client);

            // 3. Update the community
            existing.Name = req.Name ?? existing.Name;
            existing.Description = req.Description ?? existing.Description;
            existing.BannerUrl = bannerUrl;
            existing.AvatarUrl = avatarUrl;

            var updateRes = await _client.From<Community>().Update(existing);

            if (!updateRes.Models.Any())
                return BadRequest(new { error = "Failed to update community." });

            return Ok(new
            {
                message = "Community successfully updated.",
                communityId = req.CommunityId
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
                        Id = t.id,
                        Name = t.name
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
    [HttpGet("getbyid")]
    public async Task<IActionResult> GetCommunityById(string id)
    {
        try
        {
            // 1. Get the community by ID
            var communityResponse = await _client
                .From<Community>()
                .Where(c => c.Id == id)
                .Single();

            if (communityResponse == null)
                return NotFound(new { message = "Community not found." });

            // 2. Get associated topic IDs
            var communityTopics = await _client
                .From<CommunityTopic>()
                .Where(ct => ct.CommunityId == id)
                .Get();

            var topicIds = communityTopics.Models.Select(ct => ct.TopicId).ToList();

            // 3. Get topic details
            var topics = new List<TopicDto>();
            if (topicIds.Any())
            {
                var topicRecords = await _client
                    .From<Topic>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.In, topicIds)
                    .Get();

                topics = topicRecords.Models.Select(t => new TopicDto
                {
                    Id = t.id,
                    Name = t.name
                }).ToList();
            }

            // 4. Map to DTO
            var communityDto = new CommunityDto
            {
                Id = communityResponse.Id,
                AdminId = communityResponse.AdminId,
                Name = communityResponse.Name,
                Description = communityResponse.Description,
                BannerUrl = communityResponse.BannerUrl,
                AvatarUrl = communityResponse.AvatarUrl,
                CreatedAt = communityResponse.CreatedAt,
                Topics = topics
            };

            return Ok(communityDto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Cannot connect to Supabase", error = ex.Message });
        }
    }

    [HttpPost("join")]
    public async Task<IActionResult> JoinCommunity(string userId, string communityId)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(communityId))
            return BadRequest(new { error = "UserId and CommunityId are required." });

        try
        {
            // 1. Validate user exists
            var userCheck = await _client
                .From<User>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId)
                .Single();

            if (userCheck == null)
                return BadRequest(new { error = "User does not exist." });

            // 2. Validate community exists
            var communityCheck = await _client
                .From<Community>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, communityId)
                .Single();

            if (communityCheck == null)
                return BadRequest(new { error = "Community does not exist." });

            // 3. Prevent duplicate membership
            var existing = await _client
                .From<Member>()
                .Where(m => m.user_id == userId && m.community_id == communityId)
                .Get();

            if (existing.Models.Any())
                return BadRequest(new { error = "User already joined the community." });

            // 4. Insert new record
            var newMember = new Member
            {
                user_id = userId,
                community_id = communityId,
                created_at = DateTime.UtcNow,
                status = "joined"
            };

            await _client.From<Member>().Insert(newMember);

            return Ok(new
            {
                message = "User successfully joined the community.",
                userId,
                communityId
             
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("leave")]
    public async Task<IActionResult> LeaveCommunity([FromQuery] string userId, [FromQuery] string communityId)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(communityId))
            return BadRequest(new { error = "UserId and CommunityId are required." });

        try
        {
            // Delete membership using composite key
            await _client
                .From<Member>()
                .Where(m => m.user_id == userId && m.community_id == communityId)
                .Delete();

            return Ok(new
            {
                message = "User has successfully left the community.",
                userId,
                communityId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("admin kick members")]
    public async Task<IActionResult> KickMember([FromQuery] string adminId, [FromQuery] string userId, [FromQuery] string communityId)
    {
        if (string.IsNullOrWhiteSpace(adminId) ||
            string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(communityId))
        {
            return BadRequest(new { error = "AdminId, UserId, and CommunityId are required." });
        }

        try
        {
            // 1. Validate community exists
            var community = await _client
                .From<Community>()
                .Where(c => c.Id == communityId)
                .Single();

            if (community == null)
                return BadRequest(new { error = "Community does not exist." });

            // 2. Ensure caller is the admin
            if (community.AdminId?.ToString() != adminId)
                return Unauthorized(new { error = "Only the community admin can remove members." });

            // 3. Ensure the user is actually in the community
            var membership = await _client
                .From<Member>()
                .Where(m => m.user_id == userId && m.community_id == communityId)
                .Single();

            if (membership == null)
                return BadRequest(new { error = "User is not a member of this community." });

            // 4. Prevent admin from kicking themselves
            if (userId == adminId)
                return BadRequest(new { error = "Admin cannot remove themselves from their own community." });

            // 5. Delete membership
            await _client
                .From<Member>()
                .Where(m => m.user_id == userId && m.community_id == communityId)
                .Delete();

            return Ok(new
            {
                message = "User has been removed from the community.",
                removedUserId = userId,
                communityId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }


    [HttpGet("getCommunityMembers")]
    public async Task<IActionResult> GetMembersByCommunity([FromQuery] string communityId)
    {
        if (string.IsNullOrWhiteSpace(communityId))
            return BadRequest(new { error = "communityId is required." });

        try
        {
            var memberResp = await _client
                .From<Member>()
                .Filter("community_id", Supabase.Postgrest.Constants.Operator.Equals, communityId)
                .Get();

            var members = memberResp.Models;

            if (!members.Any())
                return Ok(new { message = "No members in this community yet.", members = new List<object>() });

            var userIds = members.Select(m => m.user_id).ToList();

            var userResp = await _client
                .From<User>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.In, userIds)
                .Get();

            var users = userResp.Models;

            var result = members.Select(m =>
            {
                var user = users.FirstOrDefault(u => u.id.ToString() == m.user_id);

                // map only the fields we want in DTO
                return new
                {
                    user_id = m.user_id,
                    community_id = m.community_id,
                    created_at = m.created_at,
                    status = m.status,
                    user = user != null ? new
                    {
                        id = user.id.ToString(),
                        created_at = user.created_at,
                        name = user.name,
                        avatar_url = user.avatar_url,
                        banner_url = user.banner_url,
                        email = user.email,
                        bio = user.bio
                    } : null
                };
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }






    [HttpGet("user communities")]
    public async Task<IActionResult> GetUserCommunities([FromQuery] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(new { error = "UserId is required." });

        try
        {
            // 1. Get memberships for this user
            var memberships = await _client
                .From<Member>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userId)
                .Get();

            if (!memberships.Models.Any())
                return Ok(new { message = "User has not joined any communities.", communities = new List<object>() });

            // 2. Extract community IDs
            var communityIds = memberships.Models.Select(m => m.community_id).ToList();

            // 3. Fetch communities using the IDs
            var communityResp = await _client
                .From<Community>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.In, communityIds)
                .Get();

            var communities = communityResp.Models;

            // 4. Map to DTO/anonymous object to avoid serialization issues
            var result = communities.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                description = c.Description,
                banner_url = c.BannerUrl,
                avatar_url = c.AvatarUrl,
                admin_id = c.AdminId,
                created_at = c.CreatedAt,
                is_banned = c.IsBanned
            }).ToList();

            return Ok(new
            {
                userId,
                count = result.Count,
                communities = result
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }


    [HttpPatch("ban")]
    public async Task<IActionResult> BanCommunity([FromQuery] string communityId, [FromQuery] Guid adminId)
    {
        if (string.IsNullOrWhiteSpace(communityId))
            return BadRequest(new { error = "CommunityId is required." });

        try
        {
            // 1. Get community
            var result = await _client
     .From<Community>()
     .Where(c => c.Id == communityId)
     .Get(); // Use Get() instead of Single()

            var community = result.Models.FirstOrDefault();
            if (community == null)
                return BadRequest(new { error = "Community not found." });


            
            // 2. Only the admin of the community can ban it
            if (community.AdminId != adminId)
                return BadRequest(new { error = "Only the community admin can ban this community." });

            // 3. If already banned, skip
            if (community.IsBanned == true)
                return BadRequest(new { error = "Community is already banned." });

            // 4. Update is_banned = true
            community.IsBanned = true;

            await _client
                .From<Community>()
                .Update(community);

            return Ok(new
            {
                message = "Community has been banned successfully.",
                communityId = communityId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("unban")]
    public async Task<IActionResult> UnbanCommunity([FromQuery] string communityId)
    {
        if (string.IsNullOrWhiteSpace(communityId))
            return BadRequest(new { error = "CommunityId is required." });

        try
        {
            // Check community exists
            var community = await _client
                .From<Community>()
                .Where(c => c.Id == communityId)
                .Single();

            if (community == null)
                return BadRequest(new { error = "Community does not exist." });

            // Update is_banned = false
            community.IsBanned = false;

            await _client.From<Community>().Update(community);

            return Ok(new
            {
                message = "Community has been unbanned successfully.",
                communityId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("get not banned communities")]
    public async Task<IActionResult> GetUnbannedCommunities()
    {
        try
        {
            var result = await _client
                .From<Community>()
                .Filter("is_banned", Supabase.Postgrest.Constants.Operator.Equals, false)
                .Get();

            return Ok(result.Models);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("get banned communities")]
    public async Task<IActionResult> GetbannedCommunities()
    {
        try
        {
            var result = await _client
                .From<Community>()
                .Filter("is_banned", Supabase.Postgrest.Constants.Operator.Equals, true)
                .Get();

            return Ok(result.Models);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }





}
