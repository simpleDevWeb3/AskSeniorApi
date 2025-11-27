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

    [HttpGet/*("{user_id:}")*/]
    public async Task<IActionResult> GetPost(string? user_id=null)
    {
        var query = _supabase.From<Post>().Select("*");

      

        if (string.IsNullOrWhiteSpace(user_id) ||
            user_id == "undefined" ||
            user_id == "null")
        {
            user_id = null; // Force it to real null
        }

        if (!string.IsNullOrEmpty(user_id))
        {
            query = query.Where(x => x.user_id == user_id);
        }

        var post = await query.Get();

        /*
        var user = await _supabase.From<User>().Where(u => u.id == user_id).Get();

        var topic = await _supabase
            .From<Topic>()
            .Where(t => t.id == user.)
            .Get();
       */
        var dtoData = post.Models.Select(p => new PostCreate
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

    [HttpPost("createPost")]
    public async Task<IActionResult> CreatePost([FromForm] PostCreate newPost)
    {
        var post = await _supabase.From<Post>().Select("*").Get();
        long unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();  //second since 1970
                                                                // 1. Get the raw value
        string? incomingId = newPost.community_id;

        // 2. Check for "undefined", "null", or empty space
        if (string.IsNullOrWhiteSpace(incomingId) ||
            incomingId == "undefined" ||
            incomingId == "null")
        {
            incomingId = null; // Force it to real null
        }
        var dtoData = new Post
        {
            id = "P" + unix,
            user_id = newPost.user_id,
            topic_id = newPost.topic_id,
            community_id = incomingId,
            created_at = DateTime.Now,
            title = newPost.title,
            text = newPost.text
        };
        System.Diagnostics.Debug.WriteLine($"MY DEBUG LOG: {dtoData.community_id}");
        var response = await _supabase.From<Post>().Insert(dtoData);
        //get comment and vote also
        return Ok(dtoData.id);
    }
    
    [HttpPost("{post_id}")]
    public async Task<IActionResult> EditPost(string post_id, [FromForm] PostEdit editedPost)
    {
        
        var posts = await _supabase
            .From<Post>()
            .Where(p => p.id == post_id)
            .Get();

        if (posts.Models.Count <= 0) return NotFound();
        var post = posts.Models.FirstOrDefault();
        
        var dtoData = new Post
        {
            id = post.id,
            user_id = post.user_id,
            created_at = post.created_at,
            topic_id = editedPost.topic_id,
            community_id = editedPost.community_id,
            title = editedPost.title,
            text = editedPost.text
        };

        var response = await _supabase
            .From<Post>()
            .Where(p => p.id == post_id)
            .Update(dtoData);

        return Ok(dtoData.id);
    }
    

    [HttpDelete("{post_id}")]
    public async Task<IActionResult> DeleteNewsletter(string post_id)
    {
        await _supabase
            .From<Post>()
            .Where(p => p.id == post_id)
            .Delete();

        return NoContent();
    }
}
