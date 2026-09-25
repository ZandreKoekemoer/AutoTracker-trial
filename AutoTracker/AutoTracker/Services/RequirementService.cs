using AutoTracker.Models;

namespace AutoTracker.Services
{
    public class RequirementService
    {
        private readonly CompletionService _completion = new();
        public List<string> GetMissingStartRequirements(RepairJob job) => _completion.GetMissingRequirements(job);
        public List<string> GetMissingReadyRequirements(RepairJob job) => _completion.GetMissingReadyRequirements(job);
        public bool CanStartRepair(RepairJob job) => !GetMissingStartRequirements(job).Any();
        public bool CanMarkReady(RepairJob job) => !GetMissingReadyRequirements(job).Any();
    }
}
