using System.Globalization;
using System.Text.Json;
using ScoutsReporter.Models;

namespace ScoutsReporter.Services;

public class TrainingService
{
    private static readonly HashSet<string> ExpiringTrainings = new()
    {
        "First Response", "Safeguarding", "Safety"
    };

    public static readonly Dictionary<string, int> FlagPriority = new()
    {
        ["EXPIRED"] = 0,
        ["EXPIRING SOON"] = 1,
        ["MISSING"] = 2,
        ["OK"] = 3,
        ["No expiry"] = 4,
    };

    private readonly ApiService _api;
    private readonly AuthService _auth;

    public TrainingService(ApiService api, AuthService auth)
    {
        _api = api;
        _auth = auth;
    }

    public async Task<Dictionary<string, List<JsonElement>>> FetchAllTrainingAsync(
        Dictionary<string, Member> members,
        IProgress<string>? progress = null)
    {
        var training = new Dictionary<string, List<JsonElement>>();
        var contactIds = members.Keys.ToList();
        int total = contactIds.Count;

        for (int i = 0; i < total; i++)
        {
            var cid = contactIds[i];
            var name = members[cid].FullName;

            try
            {
                var records = await _api.FetchLmsDetailsAsync(cid);
                training[cid] = records;
                var expiring = records.Count(r => !string.IsNullOrEmpty(r.GetProp("expiryDate")));
                progress?.Report($"[{i + 1}/{total}] {name}... {records.Count} training(s), {expiring} with expiry");
            }
            catch (Exception ex)
            {
                progress?.Report($"[{i + 1}/{total}] {name}... error: {ex.Message}");
                training[cid] = new();
            }

            if ((i + 1) % 15 == 0 && i + 1 < total)
            {
                await _auth.RefreshTokenAsync();
                progress?.Report("(token refreshed)");
            }
            await Task.Delay(300);
        }
        return training;
    }

    public static (string flag, string expiry, string warning) ClassifyTraining(JsonElement record)
    {
        var expiryStr = record.GetProp("expiryDate");
        if (string.IsNullOrEmpty(expiryStr))
            return ("No expiry", "", "");

        if (!DateTime.TryParseExact(expiryStr.Trim(), "M/d/yyyy h:mm:ss tt",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) &&
            !DateTime.TryParseExact(expiryStr.Trim(), "M/d/yyyy H:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
        {
            return ("Unknown", expiryStr, "");
        }

        var expiryDate = dt.ToString("yyyy-MM-dd");
        int days = (dt - DateTime.Now).Days;

        if (days < 0) return ("EXPIRED", expiryDate, $"Expired {Math.Abs(days)} days ago");
        if (days < 90) return ("EXPIRING SOON", expiryDate, $"{days} days remaining");
        return ("OK", expiryDate, $"{days} days remaining");
    }

    public static (List<TrainingReportRow> report, List<string> sortedTitles) BuildReport(
        Dictionary<string, Member> members,
        Dictionary<string, List<JsonElement>> training)
    {
        // Discover all unique training titles
        var allTitles = new HashSet<string>();
        foreach (var records in training.Values)
            foreach (var r in records)
            {
                var title = r.GetProp("title");
                if (!string.IsNullOrEmpty(title))
                    allTitles.Add(title);
            }

        var sortedTitles = allTitles
            .OrderBy(t => ExpiringTrainings.Contains(t) ? 0 : 1)
            .ThenBy(t => t)
            .ToList();

        var report = new List<TrainingReportRow>();

        foreach (var (cid, mem) in members.OrderBy(x => x.Value.FullName))
        {
            var records = training.TryGetValue(cid, out var recs) ? recs : new();

            // Build lookup by title
            var byTitle = new Dictionary<string, JsonElement>();
            foreach (var r in records)
            {
                var title = r.GetProp("title");
                if (!string.IsNullOrEmpty(title))
                    byTitle[title] = r;
            }

            bool trusteeOnly = mem.Roles.Count > 0 &&
                mem.Roles.All(role => role.Contains("Trustee Board"));

            var row = new TrainingReportRow
            {
                Name = mem.FullName,
                Roles = string.Join("; ", mem.Roles),
                TotalTrainings = records.Count,
            };

            string worstFlag = "OK";

            foreach (var title in sortedTitles)
            {
                if (byTitle.TryGetValue(title, out var r))
                {
                    var (flag, expiry, warning) = ClassifyTraining(r);
                    var level = r.GetProp("currentLevel");
                    if (!string.IsNullOrEmpty(expiry))
                    {
                        row.TrainingColumns[title] = expiry;
                        row.TrainingColumns[$"{title} Warning"] = warning;
                    }
                    else
                    {
                        row.TrainingColumns[title] = level;
                    }
                    if (FlagPriority.GetValueOrDefault(flag, 99) < FlagPriority.GetValueOrDefault(worstFlag, 99))
                        worstFlag = flag;
                }
                else
                {
                    if (ExpiringTrainings.Contains(title))
                    {
                        if (title == "First Response" && trusteeOnly)
                        {
                            row.TrainingColumns[title] = "N/A";
                        }
                        else
                        {
                            row.TrainingColumns[title] = "MISSING";
                            if (FlagPriority.GetValueOrDefault("MISSING", 99) < FlagPriority.GetValueOrDefault(worstFlag, 99))
                                worstFlag = "MISSING";
                        }
                    }
                    else
                    {
                        row.TrainingColumns[title] = "";
                    }
                }
            }

            row.Flag = worstFlag;
            report.Add(row);
        }

        report.Sort((a, b) =>
        {
            int pa = FlagPriority.GetValueOrDefault(a.Flag, 99);
            int pb = FlagPriority.GetValueOrDefault(b.Flag, 99);
            if (pa != pb) return pa.CompareTo(pb);
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });

        return (report, sortedTitles);
    }
}
