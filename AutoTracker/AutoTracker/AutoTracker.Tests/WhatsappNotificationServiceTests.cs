using AutoTracker.Services;

namespace AutoTracker.Tests;

public class WhatsappNotificationServiceTests
{
    private readonly WhatsappNotificationService _service = new();

    [Theory]
    [InlineData("082 123 4567", "27821234567")]
    [InlineData("(082)-123-4567", "27821234567")]
    [InlineData("+27 82 123 4567", "27821234567")]
    [InlineData("27821234567", "27821234567")]
    public void TryNormalizeSouthAfricanPhone_ReturnsE164Digits(string input, string expected)
    {
        var valid = _service.TryNormalizeSouthAfricanPhone(input, out var normalized);

        Assert.True(valid);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("not a phone")]
    [InlineData("2782123456789")]
    public void TryNormalizeSouthAfricanPhone_RejectsInvalidNumbers(string input)
    {
        Assert.False(_service.TryNormalizeSouthAfricanPhone(input, out _));
    }

    [Fact]
    public void BuildWhatsAppUrl_EncodesMessageAndUsesNormalizedPhone()
    {
        var url = _service.BuildWhatsAppUrl("0821234567", "Hi John, status: Paint & Prep");

        Assert.Equal("https://wa.me/27821234567?text=Hi+John%2C+status%3A+Paint+%26+Prep", url);
    }
}
