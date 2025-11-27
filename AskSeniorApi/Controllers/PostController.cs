using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AskSeniorApi.Models;
using Supabase;
using static AskSeniorApi.Models.Auth;
using AskSeniorApi.DTO;

namespace AskSeniorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PostController : ControllerBase
{
    private readonly Client _supabase;

    public PostController(Client supabase)
    {
        _supabase = supabase;
    }

    [HttpGet/*("{user_id:string}")*/]
    public async Task<IActionResult> GetPost(/*string user_id*/)
    {
        var post = await _supabase.From<Post>().Select("*").Get();
        /*
        var user = await _supabase.From<User>().Where(u => u.id == user_id).Get();

        var topic = await _supabase
            .From<Topic>()
            .Where(t => t.id == user.)
            .Get();
       */
        var dtoData = post.Models.Select(p => new PostDto
        {
            id = p.id,
            user_id = p.user_id,
            topic_id = p.topic_id,
            community_id = p.community_id,
            created_at = p.created_at,
            title = p.title,
            text = p.text
        });

        return Ok(dtoData);
    }

    [HttpPost]
    public async Task<IActionResult> PostPost([FromForm] PostDto newPost)
    {
        var post = await _supabase.From<Post>().Select("*").Get();
        long unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();  //second since 1970

        var dtoData = new Post
        {
            id = "P" + unix,
            user_id = newPost.user_id,
            topic_id = newPost.topic_id,
            community_id = newPost.community_id,
            created_at = DateTime.Now,
            title = newPost.title,
            text = newPost.text
        };

        var response = await _supabase.From<Post>().Insert(dtoData);

        return Ok(dtoData.id);
    }

    [HttpDelete("{post_id:string}")]
    public async Task<IActionResult> DeleteNewsletter(string post_id)
    {
        await _supabase
            .From<Post>()
            .Where(p => p.id == post_id)
            .Delete();

        return NoContent();
    }
}
