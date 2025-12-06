using AskSeniorApi.Models;

public static class VoteHelper
{
    public static int GetVoteCount(IEnumerable<VotedPostComment> votes, bool isPost, string? postId, string? commentId, bool up_down)
    {
        return isPost
            ? votes.Count(v => v.PostId == postId && v.IsUpvote == up_down)
            : votes.Count(v => v.CommentId == commentId && v.IsUpvote == up_down);
    }

    public static int GetCommentCount(IEnumerable<Comment> comments, bool isPost, string? postId, string? commentId)
    {
        return isPost
            ? comments.Count(c => c.PostId == postId)
            : comments.Count(c => c.Parent?.CommentId == commentId);
    }
}
