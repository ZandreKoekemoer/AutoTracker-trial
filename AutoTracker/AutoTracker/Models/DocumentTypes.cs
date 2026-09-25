namespace AutoTracker.Models;

public static class DocumentTypes
{
    public const string Authorization = "Authorization";
    public const string BeforePhotos = "Before Photos";
    public const string Checklist = "Checklist";
    public const string FinalPhotos = "Final Photos";
    public const string PartsInvoice = "Parts Invoice";
    public const string SupplierInvoice = "Supplier Invoice";
    public const string CustomerInvoice = "Customer Invoice";
    public const string LabourInvoice = "Labour Invoice";
    public const string PaintInvoice = "Paint Invoice";
    public const string ConsumablesInvoice = "Consumables Invoice";
    public const string Other = "Other";

    public static readonly string[] All =
    [
        Authorization,
        BeforePhotos,
        Checklist,
        FinalPhotos,
        PartsInvoice,
        SupplierInvoice,
        CustomerInvoice,
        LabourInvoice,
        PaintInvoice,
        ConsumablesInvoice,
        Other
    ];

    public static readonly string[] RequiredBeforeStart = [Authorization, BeforePhotos];
    public static readonly string[] RequiredBeforeReady = [CustomerInvoice, FinalPhotos];
    public static readonly string[] CostDocuments = [PartsInvoice, SupplierInvoice, LabourInvoice, PaintInvoice, ConsumablesInvoice];

    public static string Normalize(string? value)
    {
        var v = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(v)) return Other;

        var lower = v.ToLowerInvariant();
        return lower switch
        {
            "invoice" => CustomerInvoice,
            "customer invoice" => CustomerInvoice,
            "client invoice" => CustomerInvoice,
            "final invoice" => CustomerInvoice,
            "parts" => PartsInvoice,
            "part invoice" => PartsInvoice,
            "parts invoice" => PartsInvoice,
            "supplier" => SupplierInvoice,
            "supplier invoice" => SupplierInvoice,
            "sublet invoice" => SupplierInvoice,
            "sublet" => SupplierInvoice,
            "labour invoice" => LabourInvoice,
            "labor invoice" => LabourInvoice,
            "labour" => LabourInvoice,
            "labor" => LabourInvoice,
            "paint invoice" => PaintInvoice,
            "paint" => PaintInvoice,
            "consumables invoice" => ConsumablesInvoice,
            "consumables" => ConsumablesInvoice,
            "authorization" => Authorization,
            "authorisation" => Authorization,
            "authorization document" => Authorization,
            "authorisation document" => Authorization,
            "signed authorization" => Authorization,
            "signed authorisation" => Authorization,
            "checklist" => Checklist,
            "check list" => Checklist,
            "job checklist" => Checklist,
            "vehicle checklist" => Checklist,
            "full checklist" => Checklist,
            "damage photos" => BeforePhotos,
            "before" => BeforePhotos,
            "before photo" => BeforePhotos,
            "before photos" => BeforePhotos,
            "before pictures" => BeforePhotos,
            "after photos" => FinalPhotos,
            "after photo" => FinalPhotos,
            "final photos" => FinalPhotos,
            "final photo" => FinalPhotos,
            "collection photos" => FinalPhotos,
            "other" => Other,
            _ => All.FirstOrDefault(x => string.Equals(x, v, StringComparison.OrdinalIgnoreCase)) ?? Other
        };
    }

    public static bool IsCostDocument(string? value) => CostDocuments.Contains(Normalize(value));
}
