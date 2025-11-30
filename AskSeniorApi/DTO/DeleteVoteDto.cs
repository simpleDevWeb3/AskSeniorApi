using Microsoft.AspNetCore.Mvc;

namespace AskSeniorApi.DTO;
public class DeleteVoteDto
{
    public string? post_id { get; set; }
    public string? comment_id { get; set; }
    public string? user_id { get; set; }
}