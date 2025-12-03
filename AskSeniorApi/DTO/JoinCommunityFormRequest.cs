using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

public class JoinCommunityFormRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string CommunityId { get; set; }
}

