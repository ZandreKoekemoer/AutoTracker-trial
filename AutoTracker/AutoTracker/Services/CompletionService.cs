using AutoTracker.Models;

namespace AutoTracker.Services;

public class CompletionService
{
    public List<string> GetMissingRequirements(RepairJob job)
    {
        var missing = new List<string>();
        if (job.Quote is null || !job.Quote.IsCompleted) missing.Add("Quote");
        if (job.Checklist is null || !job.Checklist.IsCompleted) missing.Add("Checklist");
        foreach (var type in DocumentTypes.RequiredBeforeStart)
            if (!HasDocument(job, type)) missing.Add(type);
        return missing;
    }

    public List<string> GetMissingReadyRequirements(RepairJob job)
    {
        var missing = new List<string>();
        foreach (var type in DocumentTypes.RequiredBeforeReady)
            if (!HasDocument(job, type)) missing.Add(type);
        return missing;
    }

    public static bool HasDocument(RepairJob job, string type) =>
        job.Documents.Any(d => DocumentTypes.Normalize(d.DocumentType) == DocumentTypes.Normalize(type) && d.IsCompleted);
}
