namespace OpinionHub.Web.Models;

public class PollOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Text { get; set; } = string.Empty;
    public Guid PollId { get; set; }
    public Poll? Poll { get; set; }
    public ICollection<VoteSelection> VoteSelections { get; set; } = new List<VoteSelection>();
}
