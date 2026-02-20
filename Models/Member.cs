namespace ScoutsReporter.Models;

public class Member
{
    public string ContactId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string FullName { get; set; } = "";
    public List<string> Roles { get; set; } = new();
    public List<string> MembershipNumbers { get; set; } = new();
    public HashSet<string> UnitNames { get; set; } = new();
    public Dictionary<string, HashSet<string>> UnitTeams { get; set; } = new();
}

public class UnitInfo
{
    public string Id { get; set; } = "";
    public string UnitName { get; set; } = "";
}

public class TeamInfo
{
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string UnitId { get; set; } = "";
    public string UnitName { get; set; } = "";
}
