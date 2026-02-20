using System.Globalization;
using System.Text.Json;
using ScoutsReporter.Models;

namespace ScoutsReporter.Services;

public class DisclosureService
{
    private static readonly Dictionary<string, string> OnboardingCategoryNames = new()
    {
        ["coreLearning"] = "Core Learning",
        ["dataProtectionTrainingComplete"] = "Data Protection",
        ["managerDisclosureCheck"] = "DBS Check",
        ["managerWelcomeConversation"] = "Welcome Conversation",
        ["referenceRequest"] = "References",
        ["safeguardconfidentialEnquiryCheck"] = "CE Check",
        ["signDeclaration"] = "Declaration",
        ["updateMemberProfile"] = "Profile Update",
        ["managerTrusteeCheck"] = "Trustee Check",
    };

    public static readonly Dictionary<string, int> FlagPriority = new()
    {
        ["EXPIRED"] = 0,
        ["EXPIRING SOON"] = 1,
        ["ACTION NEEDED"] = 2,
        ["DBS IN PROGRESS"] = 3,
        ["NO DISCLOSURE"] = 4,
        ["NOT IN SYSTEM"] = 5,
        ["CHECK"] = 6,
        ["OK"] = 7,
    };

    private readonly ApiService _api;
    private readonly AuthService _auth;

    public DisclosureService(ApiService api, AuthService auth)
    {
        _api = api;
        _auth = auth;
    }

    public static void LinkMembersToOnboarding(
        Dictionary<string, Member> members,
        Dictionary<string, OnboardingInfo> onboarding)
    {
        // Build lookup by normalized full name
        var fullNameToMns = new Dictionary<string, List<string>>();
        var lastNameToMns = new Dictionary<string, List<string>>();

        foreach (var (mn, info) in onboarding)
        {
            var fnKey = NormalizeName($"{info.FirstName} {info.LastName}");
            if (!fullNameToMns.ContainsKey(fnKey))
                fullNameToMns[fnKey] = new();
            if (!fullNameToMns[fnKey].Contains(mn))
                fullNameToMns[fnKey].Add(mn);

            var lnKey = info.LastName.ToLower().Trim();
            if (!lastNameToMns.ContainsKey(lnKey))
                lastNameToMns[lnKey] = new();
            if (!lastNameToMns[lnKey].Contains(mn))
                lastNameToMns[lnKey].Add(mn);
        }

        foreach (var mem in members.Values)
        {
            var key = NormalizeName(mem.FullName);
            if (fullNameToMns.TryGetValue(key, out var matches))
            {
                mem.MembershipNumbers = new List<string>(matches);
            }
            else
            {
                var lnKey = mem.LastName.ToLower().Trim();
                if (lastNameToMns.TryGetValue(lnKey, out var lnMatches) && lnMatches.Count == 1)
                    mem.MembershipNumbers = new List<string>(lnMatches);
            }
        }
    }

    private static string NormalizeName(string name)
    {
        return string.Join(" ", name.ToLower().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    public async Task<Dictionary<string, List<JsonElement>>> FetchAllDisclosuresAsync(
        Dictionary<string, Member> members,
        IProgress<string>? progress = null)
    {
        var disclosures = new Dictionary<string, List<JsonElement>>();
        var contactIds = members.Keys.ToList();
        int total = contactIds.Count;

        for (int i = 0; i < total; i++)
        {
            var cid = contactIds[i];
            var name = members[cid].FullName;
            progress?.Report($"[{i + 1}/{total}] {name}...");

            try
            {
                var sasUrl = await _api.GetSasUrlAsync("Disclosures", cid);
                if (string.IsNullOrEmpty(sasUrl))
                {
                    progress?.Report($"[{i + 1}/{total}] {name}... no SAS URL");
                    continue;
                }
                var records = await _api.FetchTableRecordsAsync(sasUrl, "Disclosures");
                disclosures[cid] = records;
                progress?.Report($"[{i + 1}/{total}] {name}... {records.Count} record(s)");
            }
            catch (Exception ex)
            {
                progress?.Report($"[{i + 1}/{total}] {name}... error: {ex.Message}");
            }

            if ((i + 1) % 15 == 0 && i + 1 < total)
            {
                await _auth.RefreshTokenAsync();
                progress?.Report("(token refreshed)");
            }
            await Task.Delay(300);
        }
        return disclosures;
    }

    public static (string status, string cert, string type, string authority,
        string issued, string expiry, string warning, string flag)
        ClassifyDisclosure(List<JsonElement> records)
    {
        var active = records.Where(r => StatusLower(r) == "disclosure issued").ToList();
        var expired = records.Where(r => StatusLower(r) == "disclosure expired").ToList();
        var inProg = records.Where(r =>
        {
            var s = StatusLower(r);
            return s != "disclosure issued" && s != "disclosure expired" && s != "application withdrawn";
        }).ToList();

        if (active.Count > 0)
        {
            var latest = active.OrderByDescending(r => r.GetProp("ExpiryDate")).First();
            var expiry = latest.GetProp("ExpiryDate").Split('T')[0];
            var issued = latest.GetProp("IssuedDate").Split('T')[0];
            string warning = "", flag = "OK";

            if (!string.IsNullOrEmpty(expiry) &&
                DateTime.TryParseExact(expiry, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                int days = (dt - DateTime.Now).Days;
                if (days < 0) { warning = $"EXPIRED ({Math.Abs(days)} days ago)"; flag = "EXPIRED"; }
                else if (days < 180) { warning = $"EXPIRING SOON ({days} days)"; flag = "EXPIRING SOON"; }
                else { warning = $"{days} days remaining"; flag = "OK"; }
            }

            return ("Disclosure issued", latest.GetProp("CertificateReference"),
                latest.GetProp("DisclosureType"), latest.GetProp("DisclosureAuthority"),
                issued, expiry, warning, flag);
        }

        if (inProg.Count > 0)
        {
            var latest = inProg.Last();
            return (latest.GetProp("Status").Length > 0 ? latest.GetProp("Status") : "In Progress",
                "", "", latest.GetProp("DisclosureAuthority"), "", "", "", "DBS IN PROGRESS");
        }

        if (expired.Count > 0)
        {
            var latest = expired.OrderByDescending(r => r.GetProp("ExpiryDate")).First();
            var expiry = latest.GetProp("ExpiryDate").Split('T')[0];
            var issued = latest.GetProp("IssuedDate").Split('T')[0];
            string warning = "EXPIRED";

            if (!string.IsNullOrEmpty(expiry) &&
                DateTime.TryParseExact(expiry, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                int daysAgo = (DateTime.Now - dt).Days;
                warning = $"EXPIRED ({daysAgo} days ago)";
            }

            return ("Disclosure Expired", latest.GetProp("CertificateReference"),
                latest.GetProp("DisclosureType"), latest.GetProp("DisclosureAuthority"),
                issued, expiry, warning, "EXPIRED");
        }

        return ("", "", "", "", "", "", "", "");
    }

    private static string StatusLower(JsonElement r) => r.GetProp("Status").ToLower();

    public static List<string> GetOutstandingActions(
        Dictionary<string, OnboardingInfo> onboarding,
        List<string> membershipNumbers)
    {
        var outstanding = new List<string>();
        foreach (var mn in membershipNumbers)
        {
            if (!onboarding.TryGetValue(mn, out var ob)) continue;
            foreach (var (cat, statuses) in ob.Actions)
            {
                if (cat == "managerDisclosureCheck") continue;
                if (statuses.Any(s => s.Contains("Outstanding")))
                {
                    var label = OnboardingCategoryNames.TryGetValue(cat, out var name)
                        ? $"{name} Outstanding" : $"{cat} Outstanding";
                    if (!outstanding.Contains(label))
                        outstanding.Add(label);
                }
            }
        }
        return outstanding;
    }

    public static List<DbsReportRow> BuildReport(
        Dictionary<string, Member> members,
        Dictionary<string, OnboardingInfo> onboarding,
        Dictionary<string, List<JsonElement>> disclosures)
    {
        var report = new List<DbsReportRow>();

        foreach (var (cid, mem) in members.OrderBy(x => x.Value.FullName))
        {
            var mns = mem.MembershipNumbers;

            // Email from onboarding
            var email = "";
            foreach (var mn in mns)
            {
                if (onboarding.TryGetValue(mn, out var ob) && !string.IsNullOrEmpty(ob.Email))
                { email = ob.Email; break; }
            }

            // DBS statuses
            var dbsStatuses = new List<string>();
            foreach (var mn in mns)
            {
                if (onboarding.TryGetValue(mn, out var ob))
                    dbsStatuses.AddRange(ob.DbsStatuses);
            }

            string onboardingDbs;
            if (dbsStatuses.Count == 0) onboardingDbs = "No Record";
            else if (dbsStatuses.All(s => s == "Satisfactory")) onboardingDbs = "Satisfactory";
            else if (dbsStatuses.Contains("Outstanding")) onboardingDbs = "Outstanding";
            else onboardingDbs = string.Join(", ", dbsStatuses.Distinct());

            // Disclosure records
            var records = disclosures.TryGetValue(cid, out var recs) ? recs : new();
            var (status, cert, type, authority, issued, expiry, warning, flag) = ClassifyDisclosure(records);

            if (records.Count == 0 && string.IsNullOrEmpty(flag))
            {
                status = "No Disclosure";
                flag = onboardingDbs == "Outstanding" ? "ACTION NEEDED"
                    : mns.Count == 0 ? "NOT IN SYSTEM"
                    : "NO DISCLOSURE";
            }

            if (onboardingDbs == "Outstanding" && flag != "EXPIRED" && flag != "OK" && flag != "EXPIRING SOON")
                flag = "ACTION NEEDED";

            var outstanding = GetOutstandingActions(onboarding, mns);

            report.Add(new DbsReportRow
            {
                Name = mem.FullName,
                MembershipNumber = string.Join(", ", mns),
                Email = email,
                IssuedDate = issued,
                ExpiryDate = expiry,
                ExpiryWarning = warning,
                Roles = string.Join("; ", mem.Roles),
                OnboardingDbs = onboardingDbs,
                DisclosureStatus = status,
                Certificate = cert,
                Type = type,
                Authority = authority,
                TotalDisclosures = records.Count,
                Suspended = records.Any(r => StatusLower(r) == "suspended") ? "YES" : "",
                OtherOutstanding = string.Join("; ", outstanding),
                Flag = flag,
            });
        }

        report.Sort((a, b) =>
        {
            int pa = FlagPriority.GetValueOrDefault(a.Flag, 99);
            int pb = FlagPriority.GetValueOrDefault(b.Flag, 99);
            if (pa != pb) return pa.CompareTo(pb);
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });

        return report;
    }
}
