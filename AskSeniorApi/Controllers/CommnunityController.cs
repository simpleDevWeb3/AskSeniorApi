using AskSeniorApi.DTOs;
using AskSeniorApi.Helper;
using AskSeniorApi.Models;
using Microsoft.AspNetCore.Mvc;

using Supabase;
using System.Linq;
using static Supabase.Postgrest.Constants;

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
                .Filter("id", Operator.Equals, req.AdminId.ToString())
                .Get();

            var adminUser = adminCheck.Models.FirstOrDefault();
            if (adminUser == null)
                return BadRequest(new { error = "AdminId does not exist in users table." });

            // 1a. Validate admin is NOT banned
            var bannedCheck = await _client
                .From<Banned>()
                .Filter("user_id", Operator.Equals, req.AdminId.ToString())
                .Get();

            if (bannedCheck.Models.Any())
                return BadRequest(new { error = "You are banned and cannot create communities." });


            // 2. Validate UNIQUE community name (case-insensitive)
            var nameCheck = await _client
                .From<Community>()
                .Filter("name", Operator.Equals, req.Name.Trim())
                .Get();

            if (nameCheck.Models.Count > 0)
            {
                return BadRequest(new
                {
                    error = "A community with this name already exists. Please choose a different name."
                });
            }


            // 3. Auto-generate Community ID using UNIX timestamp
            long unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string communityId = "C" + unix;


            // 4. Upload files
            string? bannerUrl = null;
            string? avatarUrl = null;

            if (req.BannerFile != null)
                bannerUrl = await UploadFile.UploadFileAsync(req.BannerFile, "Banner", _client);

            if (req.AvatarFile != null)
                avatarUrl = await UploadFile.UploadFileAsync(req.AvatarFile, "Avatar", _client);


            // 5. Create community
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

            // 6. Insert related topics
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

            // 7. Auto-join the creator
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



    [HttpGet("getAll")]
    public async Task<IActionResult> GetAllCommunities([FromQuery] string? userId)

    {
        try
        {
            // 1. Get ALL non-banned communities
            var communities = await _client
                .From<Community>()
                .Filter("is_banned", Operator.Equals, "false")
                .Get();

            // 2. If userId is empty → return IsJoined = false for all
            HashSet<string> joinedCommunityIds = new HashSet<string>();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var joinedRecords = await _client
                    .From<Member>()
                    .Where(m => m.user_id == userId)
                    .Get();

                joinedCommunityIds = joinedRecords.Models
                    .Select(m => m.community_id) // string
                    .ToHashSet();
            }

            var dtoList = new List<CommunityWithJoinStatusDto>();

            foreach (var c in communities.Models)
            {
                // 3. Get topic mappings
                var communityTopics = await _client.From<CommunityTopic>()
                    .Where(ct => ct.CommunityId == c.Id)
                    .Get();

                var topicIds = communityTopics.Models.Select(ct => ct.TopicId).ToList();

                // 4. Load topics
                var topics = new List<TopicDto>();
                if (topicIds.Any())
                {
                    var topicRecords = await _client
                        .From<Topic>()
                        .Filter("id", Operator.In, topicIds)
                        .Get();

                    topics = topicRecords.Models.Select(t => new TopicDto
                    {
                        Id = t.id,
                        Name = t.name
                    }).ToList();
                }

                // 5. Construct DTO
                dtoList.Add(new CommunityWithJoinStatusDto
                {
                    Id = c.Id,                  // string
                    AdminId = (Guid)c.AdminId,        // string
                    Name = c.Name,
                    Description = c.Description,
                    BannerUrl = c.BannerUrl,
                    AvatarUrl = c.AvatarUrl,
                    CreatedAt = c.CreatedAt,
                    Topics = topics,
                    IsJoined = joinedCommunityIds.Contains(c.Id)  // string → string
                });
            }

            return Ok(dtoList);
        }
        catch (Exception ex)
        {
            return Ok(new { message = "Cannot connect to Supabase", error = ex.Message });
        }
    }





    [HttpGet("adminCommunities/{adminId}")]
    public async Task<IActionResult> GetAdminCommunities(string adminId)
    {
        try
        {
            // 1. Get all communities moderated by the admin
            var communityResp = await _client
     .From<Community>()
     .Filter("admin_Id", Supabase.Postgrest.Constants.Operator.Equals, adminId)
     .Get();

            var communities = communityResp.Models;

            if (!communities.Any())
                return Ok(new { message = "This admin does not moderate any communities.", communities = new List<object>() });

            // 2. Get community IDs that the admin is in the Member table
            var joinedRecords = await _client
                .From<Member>()
                .Where(m => m.user_id == adminId)
                .Get();

            var joinedCommunityIds = joinedRecords.Models
                .Select(m => m.community_id)
                .ToHashSet();

            // 3. Build DTO list
            var dtoList = communities.Select(c => new CommunityWithJoinStatusDto
            {
                Id = c.Id,
                AdminId = (Guid)c.AdminId,
                Name = c.Name,
                Description = c.Description,
                BannerUrl = c.BannerUrl,
                AvatarUrl = c.AvatarUrl,
                CreatedAt = c.CreatedAt,
                IsJoined = joinedCommunityIds.Contains(c.Id) // usually true
            }).ToList();

            return Ok(dtoList);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }










    [HttpGet("getById")]
    public async Task<IActionResult> GetCommunityById(
    [FromQuery] string id,
    [FromQuery] string? userId = null
)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new { message = "CommunityId is required." });

        try
        {
            // 1. Fetch the community (only if not banned)
            var response = await _client
                .From<Community>()
                .Where(c => c.Id == id && c.IsBanned == false)
                .Single();

            if (response == null)
                return NotFound(new { message = "Community not found or it is banned." });

            // 2. Get topic mappings
            var communityTopics = await _client
                .From<CommunityTopic>()
                .Where(ct => ct.CommunityId == id)
                .Get();

            var topicIds = communityTopics.Models
                .Select(ct => ct.TopicId)
                .ToList();

            // 3. Fetch topic details
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

            // 4. Determine join status (only if userId present)
            bool isJoined = false;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var membership = await _client
                    .From<Member>()
                    .Where(m => m.user_id == userId && m.community_id == id)
                    .Get();

                isJoined = membership.Models.Any();
            }

            // 5. Build DTO
            var dto = new CommunityWithJoinStatusDto
            {
                Id = response.Id,
                AdminId = (Guid)response.AdminId,
                Name = response.Name,
                Description = response.Description,
                BannerUrl = response.BannerUrl,
                AvatarUrl = response.AvatarUrl,
                CreatedAt = response.CreatedAt,
                Topics = topics,
                IsJoined = isJoined
            };

            return Ok(dto);
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
            //
            // 1. Validate user exists
            //
            var userCheck = await _client
                .From<User>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId)
                .Single();

            if (userCheck == null)
                return BadRequest(new { error = "User does not exist." });


            //
            // 2. Check if user is banned (from ANY community)
            //
            var banCheck = await _client
                .From<Banned>()          // <-- This is your banned table
                .Where(b => b.user_id == userId)
                .Get();

            if (banCheck.Models.Any())
            {
                return BadRequest(new
                {
                    error = "User is banned and cannot join any community."
                });
            }


            //
            // 3. Validate the community exists AND is not banned
            //
            var communityCheck = await _client
                .From<Community>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, communityId)
                .Single();

            if (communityCheck == null)
                return BadRequest(new { error = "Community does not exist." });

            if (communityCheck.IsBanned)
                return BadRequest(new { error = "This community is banned and cannot be joined." });


            //
            // 4. Prevent duplicate membership
            //
            var existing = await _client
                .From<Member>()
                .Where(m => m.user_id == userId && m.community_id == communityId)
                .Get();

            if (existing.Models.Any())
                return BadRequest(new { error = "User already joined the community." });


            //
            // 5. Insert membership
            //
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
            // 1. Fetch the community
            var communityResp = await _client
                .From<Community>()
                .Where(c => c.Id == communityId)
                .Single();

            if (communityResp == null)
                return NotFound(new { error = "Community not found." });

            // 2. Prevent admin from leaving their own community
            if (communityResp.AdminId.ToString() == userId)
                return BadRequest(new { error = "Admins cannot leave their own community." });

            // 3. Delete membership
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


    [HttpDelete("groupAdminKickMembers")]
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

    [HttpGet("user-unjoined-communities")]
    public async Task<IActionResult> GetUserUnjoinedCommunities([FromQuery] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(new { error = "UserId is required." });

        try
        {
            // 1. Get memberships
            var membershipsResp = await _client
                .From<Member>()
                .Filter("user_id", Operator.Equals, userId)
                .Get();

            var joinedCommunityIds = membershipsResp.Models
                .Select(m => m.community_id)
                .ToList();

            // 2. Start the community query
            var query = _client.From<Community>();

            // 3. Exclude joined communities manually using chained !=
            foreach (var id in joinedCommunityIds)
            {
                query = (Supabase.Interfaces.ISupabaseTable<Community, Supabase.Realtime.RealtimeChannel>)query.Where(c => c.Id != id);
            }

            // 4. Also exclude banned communities
            query = (Supabase.Interfaces.ISupabaseTable<Community, Supabase.Realtime.RealtimeChannel>)query.Where(c => c.IsBanned == false);

            // 5. Execute
            var communitiesResp = await query.Get();
            var communities = communitiesResp.Models;

            // 6. Format response
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









    [HttpGet("userCommunities")]
    public async Task<IActionResult> GetUserCommunities([FromQuery] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(new { error = "UserId is required." });

        try
        {
            // 1. Get all memberships for this user
            var joinedRecords = await _client
                .From<Member>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userId)
                .Get();

            if (!joinedRecords.Models.Any())
                return Ok(new { message = "User has not joined any communities.", communities = new List<CommunityWithJoinStatusDto>() });

            var joinedCommunityIds = joinedRecords.Models
                .Select(m => m.community_id)
                .ToList();

            // 2. Fetch communities that are not banned
            var communitiesResp = await _client
                .From<Community>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.In, joinedCommunityIds)
                .Filter("is_banned", Operator.Equals, "false") // Only non-banned communities
                .Get();

            var communities = communitiesResp.Models;

            var dtoList = new List<CommunityWithJoinStatusDto>();

            foreach (var c in communities)
            {
                // 3. Get associated topic IDs
                var communityTopics = await _client
                    .From<CommunityTopic>()
                    .Where(ct => ct.CommunityId == c.Id)
                    .Get();

                var topicIds = communityTopics.Models.Select(ct => ct.TopicId).ToList();

                // 4. Get topic details
                var topics = new List<TopicDto>();
                if (topicIds.Any())
                {
                    var topicRecords = await _client
                        .From<Topic>()
                        .Filter("id", Operator.In, topicIds)
                        .Get();

                    topics = topicRecords.Models.Select(t => new TopicDto
                    {
                        Id = t.id,
                        Name = t.name
                    }).ToList();
                }

                // 5. Map to DTO
                dtoList.Add(new CommunityWithJoinStatusDto
                {
                    Id = c.Id,
                    AdminId = (Guid)c.AdminId,
                    Name = c.Name,
                    Description = c.Description,
                    BannerUrl = c.BannerUrl,
                    AvatarUrl = c.AvatarUrl,
                    CreatedAt = c.CreatedAt,
                    Topics = topics,
                    IsJoined = true  // All returned communities are joined
                });
            }

            return Ok(new
            {
                userId,
                count = dtoList.Count,
                communities = dtoList
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
            // ===========================================
            // 1. VALIDATE APP ADMIN
            // ===========================================
            var userResult = await _client
                .From<User>()
                .Filter("id", Operator.Equals, adminId.ToString())
                .Get();

            var user = userResult.Models.FirstOrDefault();

            if (user == null)
                return BadRequest(new { error = "Admin user not found." });

            if (user.role?.Trim().ToLower() != "admin")
                return BadRequest(new { error = "Only app admins can ban communities." });

            // ===========================================
            // 2. FETCH COMMUNITY
            // ===========================================
            var communityResult = await _client
                .From<Community>()
                .Filter("id", Operator.Equals, communityId)
                .Get();

            var community = communityResult.Models.FirstOrDefault();

            if (community == null)
                return BadRequest(new { error = "Community not found." });

            // ===========================================
            // 3. CHECK ALREADY BANNED
            // ===========================================
            if (community.IsBanned == true)
                return BadRequest(new { error = "Community is already banned." });

            // ===========================================
            // 4. SET is_banned = TRUE
            // ===========================================
            community.IsBanned = true;

            await _client
                .From<Community>()
                .Filter("id", Operator.Equals, community.Id)
                .Update(community);

            // ===========================================
            // 5. INSERT BAN RECORD
            // ===========================================

            long unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string banId = $"BAN{unix}C";  // C = Community
            var banRecord = new Banned
            {
                id = banId,
                community_id = communityId,
                
                reason = "Banned by app admin",
                created_at = DateTime.UtcNow
            };

            await _client
                .From<Banned>()
                .Insert(banRecord);

            return Ok(new
            {
                message = "Community banned successfully.",
                communityId = communityId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }










    [HttpPatch("unban")]
    public async Task<IActionResult> UnbanCommunity([FromQuery] string communityId, [FromQuery] Guid adminId)
    {
        if (string.IsNullOrWhiteSpace(communityId))
            return BadRequest(new { error = "CommunityId is required." });

        try
        {
            // 1. Validate app admin
            var userResult = await _client
                .From<User>()
                .Filter("id", Operator.Equals, adminId.ToString())
                .Get();

            var user = userResult.Models.FirstOrDefault();
            if (user == null)
                return BadRequest(new { error = "Admin user not found." });

            if (user.role?.Trim().ToLower() != "admin")
                return BadRequest(new { error = "Only app admins can unban communities." });

            // 2. Fetch community
            var communityResult = await _client
                .From<Community>()
                .Filter("id", Operator.Equals, communityId)
                .Get();

            var community = communityResult.Models.FirstOrDefault();
            if (community == null)
                return BadRequest(new { error = "Community not found." });

            // 3. Check if already unbanned
            if (community.IsBanned == false)
                return BadRequest(new { error = "Community is already unbanned." });

            // 4. Update is_banned = false
            community.IsBanned = false;

            await _client
                .From<Community>()
                .Filter("id", Operator.Equals, community.Id)
                .Update(community);

            // 5. Delete the ban record from Banned table
            await _client
                .From<Banned>()
                .Where(b => b.community_id == communityId)
                .Delete();

            return Ok(new
            {
                message = "Community unbanned successfully.",
                communityId = communityId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }



    [HttpGet("getNotBannedCommunities")]
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
