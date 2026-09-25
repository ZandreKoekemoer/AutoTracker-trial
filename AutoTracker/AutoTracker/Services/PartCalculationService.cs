namespace AutoTracker.Services;

public class PartCalculationService
{
    private readonly JobFinanceService _finance;

    public PartCalculationService(JobFinanceService finance) => _finance = finance;

    public Task RecalculateJobCosts(int repairJobId) => _finance.RecalculateAsync(repairJobId);
}
