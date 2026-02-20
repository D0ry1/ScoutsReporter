using System.Globalization;
using System.Text.Json;
using ScoutsReporter.Models;

namespace ScoutsReporter.Services;

public class PermitService
{
    public static readonly Dictionary<string, int> FlagPriority = new()
    {
        ["EXPIRED"] = 0,
        ["EXPIRING SOON"] = 1,
        ["IN PROGRESS"] = 2,
        ["OK"] = 3,
        ["NO PERMITS"] = 4,
    };

    private readonly ApiService _api;
    private readonly AuthService _auth;

    public PermitService(ApiService api, AuthService auth)
    {
        _api = api;
        _auth = auth;
    }

    public async Task<Dictionary<string, List<JsonElement>>> FetchAllPermitsAsync(
        Dictionary<string, Member> members,
        IProgress<string>? progress = null)
    {
        var permits = new Dictionary<string, List<JsonElement>>();
        var contactIds = members.Keys.ToList();
        int total = contactIds.Count;

        for (int i = 0; i < total; i++)
        {
            var cid = contactIds[i];
            var name = members[cid].FullName;

            try
            {
                var sasUrl = await _api.GetSasUrlAsync("Permits", cid);
                if (string.IsNullOrEmpty(sasUrl))
                {
                    progress?.Report($"[{i + 1}/{total}] {name}... no SAS URL");
                    continue;
                }
                var records = await _api.FetchTableRecordsAsync(sasUrl, "Permits");
                permits[cid] = records;
                if (records.Count > 0)
                {
                    var names = string.Join(", ", records.Select(r => r.GetProp("PermitName")));
                    progress?.Report($"[{i + 1}/{total}] {name}... {records.Count} permit(s) - {names}");
                }
                else
                {
                    progress?.Report($"[{i + 1}/{total}] {name}... no permits");
                }
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
        return permits;
    }

    public static PermitInfo ClassifyPermit(JsonElement record)
    {
        var status = record.GetProp("Status");
        var expiryStr = record.GetProp("DateofExpiry").Split('T')[0];

        var info = new PermitInfo
        {
            Name = record.GetProp("PermitName"),
            Activity = record.GetProp("PermitActivity"),
            Category = record.GetProp("PermitCategory"),
            Type = record.GetProp("PermitType"),
            Status = status,
            Issued = record.GetProp("DateofIssue").Split('T')[0],
            Expiry = expiryStr,
            Restriction = record.GetProp("GrantingRestriction")
                .Replace("\r\n", " ").Replace("\n", " "),
            Assessor = record.GetProp("TechnicalAssessor"),
            Flag = "OK",
        };

        if (status.Equals("Permit Granted", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(expiryStr) &&
            DateTime.TryParseExact(expiryStr, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var dt))
        {
            int days = (dt - DateTime.Now).Days;
            if (days < 0) { info.Warning = $"EXPIRED ({Math.Abs(days)} days ago)"; info.Flag = "EXPIRED"; }
            else if (days < 180) { info.Warning = $"EXPIRING SOON ({days} days)"; info.Flag = "EXPIRING SOON"; }
            else { info.Warning = $"{days} days remaining"; info.Flag = "OK"; }
        }
        else if (!string.IsNullOrEmpty(status) &&
                 !status.Equals("Permit Granted", StringComparison.OrdinalIgnoreCase))
        {
            info.Flag = "IN PROGRESS";
        }

        return info;
    }

    public static List<JsonElement> DeduplicatePermits(List<JsonElement> records)
    {
        var byName = new Dictionary<string, List<JsonElement>>();
        foreach (var r in records)
        {
            var name = r.GetProp("PermitName");
            if (string.IsNullOrEmpty(name))
                name = r.GetProp("PermitActivity");
            if (!byName.ContainsKey(name))
                byName[name] = new();
            byName[name].Add(r);
        }

        var best = new List<JsonElement>();
        foreach (var (_, group) in byName)
        {
            var granted = group.Where(r =>
                r.GetProp("Status").Equals("Permit Granted", StringComparison.OrdinalIgnoreCase)).ToList();
            if (granted.Count > 0)
                best.Add(granted.OrderByDescending(r => r.GetProp("DateofExpiry")).First());
            else
                best.Add(group.OrderByDescending(r => r.GetProp("DateofExpiry")).First());
        }
        return best;
    }

    public static List<PermitReportRow> BuildReport(
        Dictionary<string, Member> members,
        Dictionary<string, List<JsonElement>> permits)
    {
        var report = new List<PermitReportRow>();

        foreach (var (cid, mem) in members.OrderBy(x => x.Value.FullName))
        {
            var records = permits.TryGetValue(cid, out var recs) ? recs : new();

            if (records.Count == 0)
            {
                report.Add(new PermitReportRow
                {
                    Name = mem.FullName,
                    TotalPermits = 0,
                    Flag = "NO PERMITS",
                    Roles = string.Join("; ", mem.Roles),
                });
                continue;
            }

            var deduped = DeduplicatePermits(records);
            var classified = deduped.Select(ClassifyPermit).ToList();

            string worstFlag = "OK";
            foreach (var c in classified)
            {
                if (FlagPriority.GetValueOrDefault(c.Flag, 99) < FlagPriority.GetValueOrDefault(worstFlag, 99))
                    worstFlag = c.Flag;
            }

            report.Add(new PermitReportRow
            {
                Name = mem.FullName,
                TotalPermits = classified.Count,
                Flag = worstFlag,
                Roles = string.Join("; ", mem.Roles),
                Permits = classified,
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
