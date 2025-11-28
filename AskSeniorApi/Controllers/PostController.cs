using AskSeniorApi.DTO;
using AskSeniorApi.Helper;
using AskSeniorApi.Helpers;
using AskSeniorApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using static AskSeniorApi.Models.Auth;

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


    [HttpGet("getPost")]
    public async Task<IActionResult> GetPost(string? user_id=null, string? post_title=null)
    {
        var query = _supabase.From<Post>().Select("*");

        user_id = user_id.Clean();
        post_title = post_title.Clean();

        if (!string.IsNullOrEmpty(user_id))
        {
            query = query.Where(x => x.user_id == user_id);
        }
        
        if (!string.IsNullOrEmpty(post_title))
        {
            query = query.Filter(x => x.title, Supabase.Postgrest.Constants.Operator.ILike, $"%{post_title}%");
        }

        try
        {
            var post = await query.Get();
            if (post.Models.Count <= 0) return Ok("No record found");

            var dtoData = post.Models.Select(p => new PostResponeDto
            {
                id = p.id,
                user_id = p.user_id,
                user_name = p.User.name,
                avatar_url = p.User.avatar_url,
                topic_id = p.topic_id,
                topic_name = p.Topic.name,
                community_id = p.community_id,
                title = p.title,
                text = p.text,
                postImage_url = p.PostImage?
                        .Select(img => img.image_url)   //access each image object
                        .ToList() ?? new List<string>()
            });
            
            return Ok(dtoData);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("createPost")]
    public async Task<IActionResult> CreatePost([FromForm] PostCreateDto newPost)
    {
        var post = await _supabase.From<Post>().Select("*").Get();
        long unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();  //second since 1970
                                                                // 1. Get the raw value
        string? incomingId = newPost.community_id.Clean();

        try
        {
            var dtoData_post = new Post
            {
                id = "P" + unix,
                user_id = newPost.user_id,
                topic_id = newPost.topic_id,
                community_id = incomingId,
                created_at = DateTime.Now,
                title = newPost.title,
                text = newPost.text
            };

            await _supabase.From<Post>().Insert(dtoData_post);
            
            if (newPost.image != null && newPost.image.Length > 0)
            {
                string imageUrl = await UploadFile.UploadFileAsync(newPost.image, "PostImage", _supabase);
                
                var dtoData_postImage = new PostImage
                {
                    image_id = dtoData_post.id + "IMG" + 1, //static fisrt
                    post_id = dtoData_post.id,
                    image_url = imageUrl,
                };
                
                await _supabase.From<PostImage>().Insert(dtoData_postImage);
            }

            System.Diagnostics.Debug.WriteLine($"MY DEBUG LOG: {dtoData_post.community_id}");
            //get comment and vote also
            return Ok(dtoData_post.id);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpPost("editPost/{post_id}")]
    public async Task<IActionResult> EditPost(string post_id, [FromForm] PostEditDto editedPost)
    {
        
        var posts = await _supabase
            .From<Post>()
            .Where(p => p.id == post_id)
            .Get();

        if (posts.Models.Count <= 0) return NotFound();
        var post = posts.Models.FirstOrDefault();

        try
        {
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
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }


    [HttpDelete("deletePost/{post_id}")]
    public async Task<IActionResult> DeletePost(string post_id)
    {
        try
        {
            await _supabase
                .From<Post>()
                .Where(p => p.id == post_id)
                .Delete();

            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new {error = ex.Message});
        }
    }
}
