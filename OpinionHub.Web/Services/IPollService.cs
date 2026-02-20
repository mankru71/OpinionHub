using OpinionHub.Web.Models;
using OpinionHub.Web.ViewModels;

namespace OpinionHub.Web.Services;

public interface IPollService
{
    Task<Poll> CreateDraftAsync(CreatePollViewModel model, string authorId);
    Task PublishAsync(Guid pollId, string authorId);
    Task VoteAsync(Guid pollId, string userId, IReadOnlyCollection<Guid> optionIds);
    Task<Poll?> GetPollDetailsAsync(Guid pollId);
    Task<IReadOnlyCollection<Poll>> GetFeedAsync();
    Task<byte[]> ExportCsvAsync(Guid pollId, string userId);
    Task<byte[]> ExportXlsxAsync(Guid pollId, string userId);
    Task<int> CompleteExpiredPollsAsync();
    Task<int> ArchiveOldPollsAsync(int archiveAfterDays);
}
