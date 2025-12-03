using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

public class JoinCommunityFormRequest
{
    [Required]
    [FromForm(Name = "userId")]
    public string UserId { get; set; }

    [Required]
    [FromForm(Name = "communityId")]
    public string CommunityId { get; set; }
}
