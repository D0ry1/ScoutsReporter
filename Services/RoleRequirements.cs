using System.Text.RegularExpressions;

namespace ScoutsReporter.Services;

/// <summary>
/// Which compliance items a single role (or a member's combined roles) actually requires,
/// derived from POR Spring 2026, Chapter 16 "Teams Table".
/// Safety and Safeguarding are a single POR column ("the learning everyone needs"), so they
/// always share the same value.
/// </summary>
public readonly record struct RoleRequirementSet(bool Dbs, bool Safety, bool Safeguarding, bool FirstResponse);

/// <summary>
/// Maps Scouts roles to the checks/learning they require (POR Chapter 16 Teams Table).
///
///   * President / Vice President (honorary)        -> nothing
///   * Non Member - Needs Disclosure (NMND)         -> DBS only (no learning)
///   * Trustee Board (Chair/Treasurer/Trustee/...)  -> DBS + Safety/Safeguarding, NO First Response
///   * Section Team (Beaver/Cub/Scout/...)          -> everything (incl. First Response)
///   * Lead Volunteer (GLV etc.)                    -> everything
///   * Leadership Team members / sub-teams          -> DBS + Safety/Safeguarding, NO First Response
///   * Anything not recognised                      -> require everything (safe default)
///
/// Aggregation across a member's roles: an item is required if ANY one of their current
/// roles requires it (e.g. a President who is also a Section Team Leader still needs
/// everything, because of the leader role).
///
/// Role strings follow the membership-system format: "{Role} ({RoleStatusName}) - {Team} @ {Unit}".
/// </summary>
public static class RoleRequirements
{
    private static readonly Regex RoleRe = new(
        @"^(?<role>.*?)\s*\((?<status>[^)]*)\)\s*-\s*(?<team>.*?)\s*@\s*(?<unit>.*)$",
        RegexOptions.Compiled);

    private static RoleRequirementSet Make(bool dbs, bool safetySafeguarding, bool firstResponse)
        => new(dbs, safetySafeguarding, safetySafeguarding, firstResponse);

    /// <summary>Parse "{Role} ({Status}) - {Team} @ {Unit}". Returns null if not in that format.</summary>
    public static (string Role, string Status, string Team, string Unit)? ParseRole(string? roleStr)
    {
        if (string.IsNullOrWhiteSpace(roleStr)) return null;
        var m = RoleRe.Match(roleStr.Trim());
        if (!m.Success) return null;
        return (m.Groups["role"].Value.Trim(), m.Groups["status"].Value.Trim(),
                m.Groups["team"].Value.Trim(), m.Groups["unit"].Value.Trim());
    }

    /// <summary>Requirement set for a single (roleTitle, team) per POR Chapter 16.</summary>
    public static RoleRequirementSet ForRole(string? roleTitle, string? team)
    {
        var r = (roleTitle ?? "").Trim().ToLowerInvariant();
        var t = (team ?? "").Trim().ToLowerInvariant();

        // Honorary roles need nothing.
        if (r is "president" or "vice president" or "vice-president")
            return Make(false, false, false);

        // Non Member - Needs Disclosure: a helper undertaking regulated activity, so needs a
        // criminal record check but no learning.
        if (r.Contains("non member") && r.Contains("needs disclosure"))
            return Make(true, false, false);
        // Other Non-Member helper roles (e.g. "Non Member - No disclosure"): nothing.
        if (r.Contains("non member"))
            return Make(false, false, false);

        // Youth roles (16-17): a criminal record check (PVG) only; their own Young Leader
        // learning, not the adult Safety/Safeguarding/First Response items tracked here.
        if (r is "young leader" or "young helper")
            return Make(true, false, false);

        // Designated Carer (helper supporting a young person): criminal record check only.
        if (r.Contains("designated carer"))
            return Make(true, false, false);

        // Locally Employed Staff: their criminal record check is completed/recorded by the
        // local employer (not in the membership system), so it isn't chased here; but
        // Safety + Safeguarding learning is still required. No First Response.
        if (r.Contains("locally employed staff"))
            return Make(false, true, false);

        // Trustee Board roles: DBS + Safety/Safeguarding, but NOT First Response.
        if (t.Contains("trustee board") || r is "chair" or "treasurer" or "trustee")
            return Make(true, true, false);

        // Section delivery roles: everything, including First Response.
        if (t.Contains("section team"))
            return Make(true, true, true);

        // Lead Volunteer (Group/District/County Lead Volunteer): everything.
        if (r.Contains("lead volunteer"))
            return Make(true, true, true);

        // Leadership Team members / sub-teams (non Lead Volunteer): DBS + Safety/Safeguarding,
        // but NOT First Response.
        if (t.Contains("leadership team"))
            return Make(true, true, false);

        // Unrecognised role/team -> require everything (safe default).
        return Make(true, true, true);
    }

    /// <summary>
    /// Aggregate requirements across a member's role strings. An item is required if ANY of
    /// the member's roles requires it. A member with no roles is treated as unknown -> require
    /// everything.
    /// </summary>
    public static RoleRequirementSet ForMember(IEnumerable<string>? roles)
    {
        var list = roles?.ToList() ?? new List<string>();
        if (list.Count == 0) return Make(true, true, true);

        bool dbs = false, safety = false, safeguarding = false, firstResponse = false;
        foreach (var roleStr in list)
        {
            var parsed = ParseRole(roleStr);
            var roleTitle = parsed?.Role ?? roleStr;
            var team = parsed?.Team ?? "";
            var req = ForRole(roleTitle, team);
            dbs |= req.Dbs;
            safety |= req.Safety;
            safeguarding |= req.Safeguarding;
            firstResponse |= req.FirstResponse;
        }
        return new RoleRequirementSet(dbs, safety, safeguarding, firstResponse);
    }
}
