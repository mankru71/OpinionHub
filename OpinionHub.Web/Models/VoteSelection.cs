namespace OpinionHub.Web.Models;

public class VoteSelection
{
    public Guid VoteId { get; set; }
    public Vote? Vote { get; set; }
    public Guid PollOptionId { get; set; }
    public PollOption? PollOption { get; set; }
}
