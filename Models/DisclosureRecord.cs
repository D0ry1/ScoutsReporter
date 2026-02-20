namespace ScoutsReporter.Models;

public class DisclosureRecord
{
    public string Status { get; set; } = "";
    public string CertificateReference { get; set; } = "";
    public string DisclosureType { get; set; } = "";
    public string DisclosureAuthority { get; set; } = "";
    public string IssuedDate { get; set; } = "";
    public string ExpiryDate { get; set; } = "";
}

public class DbsReportRow
{
    public string Name { get; set; } = "";
    public string MembershipNumber { get; set; } = "";
    public string Email { get; set; } = "";
    public string IssuedDate { get; set; } = "";
    public string ExpiryDate { get; set; } = "";
    public string ExpiryWarning { get; set; } = "";
    public string Roles { get; set; } = "";
    public string OnboardingDbs { get; set; } = "";
    public string DisclosureStatus { get; set; } = "";
    public string Certificate { get; set; } = "";
    public string Type { get; set; } = "";
    public string Authority { get; set; } = "";
    public int TotalDisclosures { get; set; }
    public string Suspended { get; set; } = "";
    public string OtherOutstanding { get; set; } = "";
    public string Flag { get; set; } = "";
}

public class OnboardingInfo
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public List<string> DbsStatuses { get; set; } = new();
    public Dictionary<string, List<string>> Actions { get; set; } = new();
}
