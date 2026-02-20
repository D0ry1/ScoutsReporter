namespace ScoutsReporter.Models;

public class PermitRecord
{
    public string PermitName { get; set; } = "";
    public string PermitActivity { get; set; } = "";
    public string PermitCategory { get; set; } = "";
    public string PermitType { get; set; } = "";
    public string Status { get; set; } = "";
    public string DateOfIssue { get; set; } = "";
    public string DateOfExpiry { get; set; } = "";
    public string GrantingRestriction { get; set; } = "";
    public string TechnicalAssessor { get; set; } = "";
}

public class PermitInfo
{
    public string Name { get; set; } = "";
    public string Activity { get; set; } = "";
    public string Category { get; set; } = "";
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
    public string Issued { get; set; } = "";
    public string Expiry { get; set; } = "";
    public string Warning { get; set; } = "";
    public string Restriction { get; set; } = "";
    public string Assessor { get; set; } = "";
    public string Flag { get; set; } = "OK";
}

public class PermitReportRow
{
    public string Name { get; set; } = "";
    public int TotalPermits { get; set; }
    public string Flag { get; set; } = "";
    public string Roles { get; set; } = "";
    public List<PermitInfo> Permits { get; set; } = new();
}
