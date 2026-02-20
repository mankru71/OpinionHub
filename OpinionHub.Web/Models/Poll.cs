namespace OpinionHub.Web.Models;

public enum PollStatus { Draft, Active, Completed, Archived }
public enum PollType { SingleChoice, MultipleChoice }
public enum VisibilityType { Public, Anonymous }

public class Poll
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public PollType PollType { get; set; }
    public VisibilityType VisibilityType { get; set; }
    public bool CanChangeVote { get; set; }
    public DateTime? EndDateUtc { get; set; }
    public PollStatus Status { get; set; } = PollStatus.Draft;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser? Author { get; set; }
    public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
