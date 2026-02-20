using System.ComponentModel.DataAnnotations;
using OpinionHub.Web.Models;

namespace OpinionHub.Web.ViewModels;

public class CreatePollViewModel
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public PollType PollType { get; set; }
    public VisibilityType VisibilityType { get; set; }
    public bool CanChangeVote { get; set; }

    public DateTime? EndDateUtc { get; set; }

    [MinLength(2, ErrorMessage = "Нужно минимум 2 варианта")]
    public List<string> Options { get; set; } = new() { "", "" };

    public bool PublishNow { get; set; }
}
