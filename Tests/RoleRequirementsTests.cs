using ScoutsReporter.Services;
using Xunit;

namespace ScoutsReporter.Tests;

/// <summary>
/// POR Spring 2026, Chapter 16 "Teams Table" truth table for RoleRequirements.
/// Role strings use the membership-system format: "{Role} ({Status}) - {Team} @ {Unit}".
/// </summary>
public class RoleRequirementsTests
{
    private const string Unit = "20th Bolton South (St Thomas Chequerbent)";

    private static string R(string role, string status, string team, string unit = Unit)
        => $"{role} ({status}) - {team} @ {unit}";

    private static RoleRequirementSet Req(params string[] roles)
        => RoleRequirements.ForMember(roles);

    // ── Honorary roles: need nothing ──
    [Fact]
    public void President_RequiresNothing()
    {
        var r = Req(R("President", "Full", "Helpers"));
        Assert.Equal(new RoleRequirementSet(false, false, false, false), r);
    }

    [Fact]
    public void VicePresident_RequiresNothing()
    {
        var r = Req(R("Vice President", "Full", "Helpers"));
        Assert.Equal(new RoleRequirementSet(false, false, false, false), r);
    }

    // ── NMND: DBS only ──
    [Fact]
    public void Nmnd_RequiresDbsOnly()
    {
        // Kevin's exact live role string (lowercase "disclosure", Provisional).
        var r = Req(R("Non Member - Needs disclosure", "Provisional", "Helpers"));
        Assert.True(r.Dbs);
        Assert.False(r.Safety);
        Assert.False(r.Safeguarding);
        Assert.False(r.FirstResponse);
    }

    // ── Trustee Board: DBS + Safety/Safeguarding, NOT First Response ──
    [Fact]
    public void Trustee_NoFirstResponse()
    {
        var r = Req(R("Trustee", "Full", "Trustee Board"));
        Assert.Equal(new RoleRequirementSet(true, true, true, false), r);
    }

    [Theory]
    [InlineData("Chair")]
    [InlineData("Treasurer")]
    public void ChairAndTreasurer_LikeTrustee(string role)
    {
        var r = Req(R(role, "Full", "Trustee Board"));
        Assert.Equal(new RoleRequirementSet(true, true, true, false), r);
    }

    // ── Section delivery roles: everything ──
    [Fact]
    public void SectionTeamMember_RequiresEverything()
    {
        var r = Req(R("Team Member", "Full", "Beaver Section Team"));
        Assert.Equal(new RoleRequirementSet(true, true, true, true), r);
    }

    [Fact]
    public void SectionTeamLeader_RequiresFirstResponse()
    {
        var r = Req(R("Team Leader", "Full", "Scout Section Team"));
        Assert.True(r.FirstResponse);
    }

    // ── Lead Volunteer (GLV): everything ──
    [Fact]
    public void GroupLeadVolunteer_RequiresFirstResponse()
    {
        var r = Req(R("Group Lead Volunteer", "Full", "Leadership Team"));
        Assert.True(r.Dbs);
        Assert.True(r.FirstResponse);
    }

    // ── Leadership Team member (non-GLV): DBS + S/S, NOT First Response ──
    [Fact]
    public void LeadershipTeamMember_NoFirstResponse()
    {
        var r = Req(R("Team Member", "None", "Leadership Team"));
        Assert.True(r.Dbs);
        Assert.True(r.Safeguarding);
        Assert.False(r.FirstResponse);
    }

    // ── Multi-role aggregation: required if ANY role requires it ──
    [Fact]
    public void PresidentPlusSectionLeader_RequiresEverything()
    {
        var r = Req(
            R("President", "Full", "Helpers"),
            R("Team Leader", "Full", "Cub Section Team"));
        Assert.Equal(new RoleRequirementSet(true, true, true, true), r);
    }

    [Fact]
    public void NmndPlusTrustee()
    {
        var r = Req(
            R("Non Member - Needs disclosure", "Full", "Helpers"),
            R("Trustee", "Full", "Trustee Board"));
        Assert.True(r.Dbs);
        Assert.True(r.Safeguarding);
        Assert.False(r.FirstResponse);
    }

    // ── Youth roles (16-17): criminal record (PVG) only, no adult learning ──
    [Theory]
    [InlineData("Young Leader", "Scout Section Team")]
    [InlineData("Young Helper", "Beaver Section Team")]
    public void YouthRoles_DbsOnly(string role, string team)
    {
        var r = Req(R(role, "Full", team));
        Assert.Equal(new RoleRequirementSet(true, false, false, false), r);
    }

    // ── Designated Carer: DBS only ──
    [Fact]
    public void DesignatedCarer_DbsOnly()
    {
        var r = Req(R("Designated Carer", "Full", "Helpers"));
        Assert.Equal(new RoleRequirementSet(true, false, false, false), r);
    }

    // ── Locally Employed Staff: DBS by employer (not here); Safety/Safeguarding yes; no FR ──
    [Fact]
    public void LocallyEmployedStaff()
    {
        var r = Req(R("Locally Employed Staff", "Full", "Leadership Team"));
        Assert.False(r.Dbs);
        Assert.True(r.Safety);
        Assert.True(r.Safeguarding);
        Assert.False(r.FirstResponse);
    }

    // ── Unknown role: require everything (safe default) ──
    [Fact]
    public void UnknownRole_RequiresEverything()
    {
        var r = Req(R("Galactic Overlord", "Full", "Mystery Team"));
        Assert.Equal(new RoleRequirementSet(true, true, true, true), r);
    }
}
