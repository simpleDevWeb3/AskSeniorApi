using AskSeniorApi.DTO;
using AskSeniorApi.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using AskSeniorApi.Helper;
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
        //get all topics
        var topics = await _supabaseClient.From<Topic>().Get();
        // [ ] topic 
        // 2. Map to DTO
        var dtoData = topics.Models.Select(t => new TopicDto
        {
            id = t.id,
            name = t.name,
            parent_id = t.parent_id,
            created_at = t.created_at 

        }).ToList();

        //build hierarchy
        var result = BuildSubHelper.BuildHierarchy<TopicDto>(
            dtoData,
            getParentId: t => t.parent_id,
            getId: t => t.id!,
            setChildren: (parent, children) => parent.sub_topic = children
        );

       
        // 3. Return safe JSON
        return Ok(result); // 200 ok 
    }

  
}
