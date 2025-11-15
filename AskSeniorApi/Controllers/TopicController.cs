using AskSeniorApi.DTO;
using AskSeniorApi.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase;

namespace AskSeniorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TopicController : ControllerBase
{
    private readonly Client _supabaseClient;

    public TopicController(Client supabaseClient)
    {
        _supabaseClient = supabaseClient;
    }

    
    [HttpGet]
    public async Task<IActionResult> GetAllTopics()
    {
        // 1. Query database via Supabase model
        var result = await _supabaseClient.From<Topic>().Select("*").Get();

        // 2. Map to DTO
        var dtoList = result.Models.Select(t => new TopicDto
        {
            id = t.id,
            name = t.name,
            parent_id = t.parent_id,
            created_at = t.created_at

        }).ToList();

        // 3. Return safe JSON
        return Ok(dtoList);
    }

  
}
