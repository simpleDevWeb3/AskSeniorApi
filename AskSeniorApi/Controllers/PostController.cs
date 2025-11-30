using AskSeniorApi.DTO;
using AskSeniorApi.Helper;
using AskSeniorApi.Helpers;
using AskSeniorApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Supabase;
using System.ComponentModel;
using static AskSeniorApi.Models.Auth;
using static Supabase.Postgrest.Constants;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
    public async Task<IActionResult> GetPost(string? user_id=null, string? post_title=null, int page = 1,int pageSize = 10)
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
            query = query.Filter(x => x.title, Operator.ILike, $"%{post_title}%");
        }

        try
        {
            // Calculate row positions (Supabase Range is inclusive)
            int from = (page - 1) * pageSize;      // 0 for page 1
            int to = (page * pageSize) - 1;      // 9 for page 1

            var post = await query
                .Order("created_at", Ordering.Descending)
                .Range(from, to)
                .Get();

            if (post.Models.Count <= 0) return Ok(new List<PostResponeDto>()); ;

            List<int> total_comment = [];
            List<int> total_upVote = [];
            List<int> total_downVote = [];
            int total = 0;

            foreach (var p in post.Models)
            {
                total = await _supabase
                    .From<Comment>()
                    .Where(c => c.PostId == p.id)
                    .Count(CountType.Exact);
                total_comment.Add(total);

                total = await _supabase
                    .From<Vote>()
                    .Where(v => v.PostId == p.id && v.IsUpvote == true)
                    .Where(v => v.CommentId == null)
                    .Count(CountType.Exact);
                total_upVote.Add(total);
                
                total = await _supabase
                    .From<Vote>()
                    .Where(v => v.PostId == p.id && v.IsUpvote == false)
                    .Where(v => v.CommentId == null)
                    .Count(CountType.Exact);
                total_downVote.Add(total);
            }

            var dtoData = post.Models.Select(p => new PostResponeDto
            {
                id = p.id,
                user_id = p.user_id,
                user_name = p.User.name,
                avatar_url = p.User.avatar_url,
                topic_id = p.topic_id,
                topic_name = p.Topic.name,
                community_id = p.community_id,
                community_name = p.Community == null ? null : p.Community.Name,
                created_at = p.created_at,
                title = p.title,
                text = p.text,
                postImage_url = p.PostImage?
                                .Select(img => img.image_url)   //access each image object
                                .ToList() ?? new List<string>(),
            }).ToList();

            for (int i = 0; i < dtoData.Count; i++)
            {
                dtoData[i].total_comment = total_comment[i];  
                dtoData[i].total_upVote = total_upVote[i];  
                dtoData[i].total_downVote = total_downVote[i];   
            }



            return Ok(dtoData);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, inner = ex.InnerException?.Message });
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
                int i = 0;
                foreach (var file in newPost.image)
                {
                    if (file.Length > 0)
                    {
                        string url = await UploadFile.UploadFileAsync(file, "PostImage", _supabase);

                        var dtoData_postImage = new PostImage
                        {
                            image_id = dtoData_post.id + "IMG" + i++, 
                            post_id = dtoData_post.id,
                            image_url = url,
                        };
                            
                        await _supabase.From<PostImage>().Insert(dtoData_postImage);
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine($"MY DEBUG LOG: {dtoData_post.community_id}");
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
