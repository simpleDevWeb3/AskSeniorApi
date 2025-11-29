namespace AskSeniorApi.DTO;
public class CommentEditDto
{
    public string? comment_id { get; set; }
    public string? user_id { get; set;} 
    public string Content { get; set; }
    public string? parent_id { get; set; }
}
