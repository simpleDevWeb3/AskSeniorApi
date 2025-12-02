using AskSeniorApi.DTO;
using AskSeniorApi.Helper;
using AskSeniorApi.Helpers;
using AskSeniorApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Supabase;
using static Supabase.Postgrest.Constants;
namespace AskSeniorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PostController : ControllerBase
{
    private readonly Client _supabase;
    private readonly ICommentService _commentService;

    public PostController(Client supabase, ICommentService commentService)
    {
        _supabase = supabase;
        _commentService = commentService;
    }


    [HttpGet("getPost")]
    public async Task<IActionResult> GetPost(string? current_user = null, 
                                            string? user_id = null,
                                            string? post_title = null,
                                            string? post_id = null,
                                            int page = 1,
                                            int pageSize = 10)
    {
        try
        {
            var query = _supabase.From<Post>()
                 .Select("*, comment(*), vote(*)");

            user_id = user_id.Clean();
            post_id = post_id.Clean();
            post_title = post_title.Clean();
            List<CommentDto> comments = [];

            if (!string.IsNullOrEmpty(user_id))
            {
                query = query.Where(x => x.user_id == user_id);
            }
        
            if (!string.IsNullOrEmpty(post_title))
            {
                query = query.Filter(x => x.title, Operator.ILike, $"%{post_title}%");
            }

            if (!string.IsNullOrEmpty(post_id))
            {
                post_id = post_id.ToUpper();    //ToUpper again after clean() ...
                query = query.Where(x => x.id == post_id);
                comments = await _commentService.GetCommentsAsync(post_id);
            }

            // Calculate row positions (Supabase Range is inclusive)
            int from = (page - 1) * pageSize;      // 0 for page 1
            int to = (page * pageSize) - 1;      // 9 for page 1

            var post = await query
                 .Order("created_at", Ordering.Descending)
                 .Range(from, to)
                 .Get();

            if (post.Models.Count <= 0) return Ok(new List<PostResponeDto>()); ;

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

                total_comment = p.comment?.Count ?? 0,
                total_upVote = p.vote?.Count(v => v.IsUpvote && v.CommentId == null) ?? 0,
                total_downVote = p.vote?.Count(v => !v.IsUpvote && v.CommentId == null) ?? 0,
                self_vote = p.vote?
                            .Where(v => v.UserId == current_user && v.CommentId == null)
                            .Select(v => (bool?)v.IsUpvote)   // cast to nullable bool
                            .FirstOrDefault(),
                Comment = comments
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
        long unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();  //second since 1970
                                                                // 1. Get the raw value
        string? incomingId = newPost.community_id.Clean();

        try
        {
            var dtoData_post = new PostInsert
            {
                id = "P" + unix,
                user_id = newPost.user_id,
                topic_id = newPost.topic_id,
                community_id = incomingId,
                created_at = DateTime.Now,
                title = newPost.title,
                text = newPost.text
            };

            await _supabase.From<PostInsert>().Insert(dtoData_post);

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
                topic_id = editedPost.topic_id ?? post.topic_id,
                community_id = editedPost.community_id ?? post.community_id,
                title = editedPost.title ?? post.title,
                text = editedPost.text ?? post.text
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

    [HttpPost("editPost/{post_id}")]
    public async Task<IActionResult> BanPost(string post_id, [FromForm] PostEditDto editedPost)
    {
        try
        {

            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }


}
