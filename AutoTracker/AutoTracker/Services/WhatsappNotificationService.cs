using AutoTracker.Models;
using System.Net;

namespace AutoTracker.Services;

public class WhatsappNotificationService
{
    public string BuildStatusMessage(RepairJob job, string clientNote, string? trackerUrl = null)
    {
        var clientName = FirstName(job.Client?.FullName);
        var registration = string.IsNullOrWhiteSpace(job.Vehicle?.RegNumber) ? "your vehicle" : job.Vehicle.RegNumber.Trim();
        var message = $"Hi {clientName}, your vehicle {registration} is currently in the {job.Status} stage. We will keep you updated as the repair progresses.";

        if (!string.IsNullOrWhiteSpace(clientNote))
            message += $" {clientNote.Trim()}";

        if (!string.IsNullOrWhiteSpace(trackerUrl))
            message += $" Track your repair here: {trackerUrl}";

        return message;
    }

    public string BuildTrackingMessage(RepairJob job, string trackerUrl, DateTime? expiresAtUtc)
    {
        var clientName = FirstName(job.Client?.FullName);
        var registration = string.IsNullOrWhiteSpace(job.Vehicle?.RegNumber) ? "your vehicle" : job.Vehicle.RegNumber.Trim();
        var message = $"Hi {clientName}, you can track the progress of your vehicle {registration} using the link below:\n{trackerUrl}\nThis link is for your repair only.";

        if (expiresAtUtc.HasValue)
            message += $" It expires on {expiresAtUtc.Value.ToLocalTime():dd MMM yyyy}.";

        return message;
    }

    public bool TryNormalizeSouthAfricanPhone(string? phone, out string normalized)
    {
        normalized = new string((phone ?? "").Where(char.IsDigit).ToArray());

        if (normalized.StartsWith("0", StringComparison.Ordinal) && normalized.Length == 10)
            normalized = "27" + normalized[1..];
        else if (normalized.StartsWith("0027", StringComparison.Ordinal) && normalized.Length == 13)
            normalized = normalized[2..];

        if (normalized.Length != 11 || !normalized.StartsWith("27", StringComparison.Ordinal))
        {
            normalized = "";
            return false;
        }

        return true;
    }

    public string BuildWhatsAppUrl(string phone, string message)
    {
        return TryNormalizeSouthAfricanPhone(phone, out var normalized)
            ? $"https://wa.me/{normalized}?text={WebUtility.UrlEncode(message ?? "")}" 
            : "";
    }

    private static string FirstName(string? fullName)
    {
        var value = (fullName ?? "there").Trim();
        if (string.IsNullOrWhiteSpace(value)) return "there";
        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
    }
}
