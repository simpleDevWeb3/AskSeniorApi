using Microsoft.AspNetCore.Mvc;
using Supabase;
using AskSeniorApi.Models;
using AskSeniorApi.DTOs;

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

    // CREATE
    [HttpPost]
    public async Task<IActionResult> CreateCommunity([FromBody] CreateCommunityDto dto)
    {
        var newCommunity = new Community
        {
            Id = dto.Id,
            AdminId = dto.AdminId,
            Name = dto.Name,
            Description = dto.Description,
            BannerUrl = dto.BannerUrl,
            AvatarUrl = dto.AvatarUrl,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _client.From<Community>().Insert(newCommunity);

        return Ok(result.Models.First());
    }

    

    // READ BY ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCommunity(string id)
    {
        var result = await _client.From<Community>()
            .Where(c => c.Id == id)
            .Single();

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    // UPDATE
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCommunity(string id, [FromBody] UpdateCommunityDto dto)
    {
        var community = await _client.From<Community>()
            .Where(c => c.Id == id)
            .Single();

        if (community == null)
            return NotFound();

        if (dto.Name != null) community.Name = dto.Name;
        if (dto.Description != null) community.Description = dto.Description;
        if (dto.BannerUrl != null) community.BannerUrl = dto.BannerUrl;
        if (dto.AvatarUrl != null) community.AvatarUrl = dto.AvatarUrl;

        var result = await _client.From<Community>().Update(community);

        return Ok(result.Models.First());
    }

    // DELETE
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCommunity(string id)
    {
        var community = await _client.From<Community>()
            .Where(c => c.Id == id)
            .Single();

        if (community == null)
            return NotFound();

        await _client.From<Community>().Delete(community);

        return Ok(new { message = "Community deleted successfully" });
    }

    // GET: api/community
    [HttpGet]
    public async Task<IActionResult> GetAllCommunities()
    {
        try
        {
            var response = await _client.From<Community>().Get();

            var dtoList = response.Models.Select(c => new CommunityDto
            {
                Id = c.Id,
                AdminId = c.AdminId,
                Name = c.Name,
                Description = c.Description,
                BannerUrl = c.BannerUrl,
                AvatarUrl = c.AvatarUrl,
                CreatedAt = c.CreatedAt
            }).ToList();

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
