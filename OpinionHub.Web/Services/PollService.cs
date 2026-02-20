using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using OpinionHub.Web.Data;
using OpinionHub.Web.Models;
using OpinionHub.Web.ViewModels;

namespace OpinionHub.Web.Services;

public class PollService : IPollService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PollService> _logger;

    public PollService(ApplicationDbContext db, ILogger<PollService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Poll> CreateDraftAsync(CreatePollViewModel model, string authorId)
    {
        if (model.EndDateUtc.HasValue && model.EndDateUtc.Value <= DateTime.UtcNow)
            throw new InvalidOperationException("Дата окончания должна быть в будущем.");

        var options = model.Options.Where(o => !string.IsNullOrWhiteSpace(o)).Distinct().ToList();
        if (options.Count < 2) throw new InvalidOperationException("Нужно минимум 2 уникальных варианта.");

        var poll = new Poll
        {
            Title = model.Title.Trim(),
            PollType = model.PollType,
            VisibilityType = model.VisibilityType,
            CanChangeVote = model.CanChangeVote,
            EndDateUtc = model.EndDateUtc,
            AuthorId = authorId,
            Status = model.PublishNow ? PollStatus.Active : PollStatus.Draft,
            Options = options.Select(o => new PollOption { Text = o.Trim() }).ToList()
        };

        _db.Polls.Add(poll);
        _db.AuditLogs.Add(new AuditLog { EventType = "POLL_CREATED", PollId = poll.Id, UserId = authorId, Details = poll.Title });
        await _db.SaveChangesAsync();
        return poll;
    }

    public async Task PublishAsync(Guid pollId, string authorId)
    {
        var poll = await _db.Polls.FirstOrDefaultAsync(p => p.Id == pollId && p.AuthorId == authorId);
        if (poll is null) throw new InvalidOperationException("Опрос не найден.");
        if (poll.Status != PollStatus.Draft) throw new InvalidOperationException("Публикуются только черновики.");
        poll.Status = PollStatus.Active;
        await _db.SaveChangesAsync();
    }

    public async Task VoteAsync(Guid pollId, string userId, IReadOnlyCollection<Guid> optionIds)
    {
        var poll = await _db.Polls.Include(p => p.Options).FirstOrDefaultAsync(p => p.Id == pollId);
        if (poll is null) throw new InvalidOperationException("Опрос не найден");
        if (poll.Status != PollStatus.Active) throw new InvalidOperationException("Голосование недоступно");
        if (poll.EndDateUtc.HasValue && poll.EndDateUtc.Value <= DateTime.UtcNow) throw new InvalidOperationException("Срок истек");
        if (poll.PollType == PollType.SingleChoice && optionIds.Count != 1) throw new InvalidOperationException("Нужно выбрать 1 вариант");
        if (optionIds.Count == 0) throw new InvalidOperationException("Выберите хотя бы один вариант");

        var validOptionIds = poll.Options.Select(o => o.Id).ToHashSet();
        if (optionIds.Any(o => !validOptionIds.Contains(o))) throw new InvalidOperationException("Некорректный вариант ответа");

        // Важный участок: мы не создаем новый голос при пере-голосовании, чтобы сохранить гарантию
        // "один голос на аккаунт", а обновляем существующую запись и фиксируем это в аудит-логе.
        var existing = await _db.Votes.Include(v => v.Selections)
            .FirstOrDefaultAsync(v => v.PollId == pollId && v.UserId == userId);

        if (existing is not null && !poll.CanChangeVote)
            throw new InvalidOperationException("Изменение голоса запрещено автором");

        if (existing is null)
        {
            existing = new Vote { PollId = pollId, UserId = poll.VisibilityType == VisibilityType.Anonymous ? null : userId };
            _db.Votes.Add(existing);
        }
        else
        {
            _db.VoteSelections.RemoveRange(existing.Selections);
        }

        existing.Selections = optionIds.Select(id => new VoteSelection { VoteId = existing.Id, PollOptionId = id }).ToList();

        _db.AuditLogs.Add(new AuditLog
        {
            EventType = "VOTE_SUBMITTED",
            PollId = pollId,
            UserId = poll.VisibilityType == VisibilityType.Anonymous ? null : userId,
            Details = $"Options={string.Join(',', optionIds)}"
        });

        await _db.SaveChangesAsync();
        _logger.LogInformation("Vote saved for poll {PollId} by {UserId}", pollId, userId);
    }

    public Task<Poll?> GetPollDetailsAsync(Guid pollId) =>
        _db.Polls.Include(p => p.Options).Include(p => p.Votes).ThenInclude(v => v.Selections).FirstOrDefaultAsync(p => p.Id == pollId);

    public async Task<IReadOnlyCollection<Poll>> GetFeedAsync() =>
        await _db.Polls.Include(p => p.Options)
            .OrderBy(p => p.Status == PollStatus.Active ? 0 : 1)
            .ThenByDescending(p => p.CreatedAtUtc)
            .ToListAsync();

    public async Task<byte[]> ExportCsvAsync(Guid pollId, string userId)
    {
        var poll = await EnsureExportAccess(pollId, userId);
        var sb = new StringBuilder();
        sb.AppendLine("Option,Votes,Percent");
        var total = poll.Votes.Count;
        foreach (var option in poll.Options)
        {
            var count = poll.Votes.Count(v => v.Selections.Any(s => s.PollOptionId == option.Id));
            var pct = total == 0 ? 0 : count * 100.0 / total;
            sb.AppendLine($"\"{option.Text}\",{count},{pct:F2}");
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportXlsxAsync(Guid pollId, string userId)
    {
        var poll = await EnsureExportAccess(pollId, userId);
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Results");
        ws.Cell(1, 1).Value = "Option";
        ws.Cell(1, 2).Value = "Votes";
        ws.Cell(1, 3).Value = "Percent";
        var total = poll.Votes.Count;

        for (var i = 0; i < poll.Options.Count; i++)
        {
            var option = poll.Options.ElementAt(i);
            var count = poll.Votes.Count(v => v.Selections.Any(s => s.PollOptionId == option.Id));
            var pct = total == 0 ? 0 : count * 100.0 / total;
            ws.Cell(i + 2, 1).Value = option.Text;
            ws.Cell(i + 2, 2).Value = count;
            ws.Cell(i + 2, 3).Value = pct;
        }

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<int> CompleteExpiredPollsAsync()
    {
        var now = DateTime.UtcNow;
        var polls = await _db.Polls.Where(p => p.Status == PollStatus.Active && p.EndDateUtc != null && p.EndDateUtc <= now).ToListAsync();
        foreach (var poll in polls)
        {
            poll.Status = PollStatus.Completed;
            poll.CompletedAtUtc = now;
            _db.AuditLogs.Add(new AuditLog { EventType = "POLL_COMPLETED", PollId = poll.Id, Details = "Auto complete" });
        }
        await _db.SaveChangesAsync();
        return polls.Count;
    }

    public async Task<int> ArchiveOldPollsAsync(int archiveAfterDays)
    {
        var threshold = DateTime.UtcNow.AddDays(-archiveAfterDays);
        var polls = await _db.Polls.Where(p => p.Status == PollStatus.Completed && p.CompletedAtUtc < threshold).ToListAsync();
        foreach (var poll in polls)
        {
            poll.Status = PollStatus.Archived;
            _db.AuditLogs.Add(new AuditLog { EventType = "POLL_ARCHIVED", PollId = poll.Id, Details = "Auto archive" });
        }
        await _db.SaveChangesAsync();
        return polls.Count;
    }

    private async Task<Poll> EnsureExportAccess(Guid pollId, string userId)
    {
        var poll = await _db.Polls.Include(p => p.Options).Include(p => p.Votes).ThenInclude(v => v.Selections).FirstOrDefaultAsync(p => p.Id == pollId);
        if (poll is null) throw new InvalidOperationException("Опрос не найден");
        if (poll.AuthorId != userId) throw new UnauthorizedAccessException("Экспорт доступен только автору");
        return poll;
    }
}
