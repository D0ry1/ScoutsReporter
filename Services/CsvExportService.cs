using System.IO;
using System.Text;
using ScoutsReporter.Models;

namespace ScoutsReporter.Services;

public static class CsvExportService
{
    public static string ExportDbsReport(List<DbsReportRow> report, string filePath)
    {
        var sb = new StringBuilder();
        // UTF-8 BOM
        var columns = new[]
        {
            "Name", "Membership Number", "Email", "Issued Date", "Expiry Date",
            "Expiry Warning", "Roles", "Onboarding DBS", "Disclosure Status",
            "Certificate", "Type", "Authority", "Total Disclosures", "Suspended",
            "Other Outstanding", "Flag"
        };

        sb.AppendLine(string.Join(",", columns.Select(Escape)));

        foreach (var r in report)
        {
            var vals = new[]
            {
                r.Name, r.MembershipNumber, r.Email, r.IssuedDate, r.ExpiryDate,
                r.ExpiryWarning, r.Roles, r.OnboardingDbs, r.DisclosureStatus,
                r.Certificate, r.Type, r.Authority, r.TotalDisclosures.ToString(),
                r.Suspended, r.OtherOutstanding, r.Flag
            };
            sb.AppendLine(string.Join(",", vals.Select(Escape)));
        }

        File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true));
        return filePath;
    }

    public static string ExportTrainingReport(List<TrainingReportRow> report, List<string> sortedTitles, string filePath)
    {
        var expiringTrainings = new HashSet<string> { "First Response", "Safeguarding", "Safety" };
        var sb = new StringBuilder();

        var columns = new List<string> { "Name", "Total Trainings" };
        foreach (var title in sortedTitles)
        {
            columns.Add(title);
            if (expiringTrainings.Contains(title))
                columns.Add($"{title} Warning");
        }
        columns.Add("Flag");
        columns.Add("Roles");

        sb.AppendLine(string.Join(",", columns.Select(Escape)));

        foreach (var r in report)
        {
            var vals = new List<string> { r.Name, r.TotalTrainings.ToString() };
            foreach (var title in sortedTitles)
            {
                vals.Add(r.TrainingColumns.GetValueOrDefault(title, ""));
                if (expiringTrainings.Contains(title))
                    vals.Add(r.TrainingColumns.GetValueOrDefault($"{title} Warning", ""));
            }
            vals.Add(r.Flag);
            vals.Add(r.Roles);
            sb.AppendLine(string.Join(",", vals.Select(Escape)));
        }

        File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true));
        return filePath;
    }

    public static string ExportPermitsReport(List<PermitReportRow> report, string filePath)
    {
        var sb = new StringBuilder();

        int maxPermits = report.Count > 0
            ? Math.Max(report.Max(r => r.TotalPermits), 1) : 1;

        var columns = new List<string> { "Name", "Total Permits" };
        for (int i = 1; i <= maxPermits; i++)
        {
            var prefix = $"Permit {i} ";
            columns.AddRange(new[]
            {
                $"{prefix}Name", $"{prefix}Type", $"{prefix}Category",
                $"{prefix}Status", $"{prefix}Issued", $"{prefix}Expiry",
                $"{prefix}Warning", $"{prefix}Restriction", $"{prefix}Assessor"
            });
        }
        columns.Add("Flag");
        columns.Add("Roles");

        sb.AppendLine(string.Join(",", columns.Select(Escape)));

        foreach (var r in report)
        {
            var vals = new List<string> { r.Name, r.TotalPermits.ToString() };
            for (int i = 0; i < maxPermits; i++)
            {
                if (i < r.Permits.Count)
                {
                    var p = r.Permits[i];
                    vals.AddRange(new[] { p.Name, p.Type, p.Category, p.Status,
                        p.Issued, p.Expiry, p.Warning, p.Restriction, p.Assessor });
                }
                else
                {
                    vals.AddRange(Enumerable.Repeat("", 9));
                }
            }
            vals.Add(r.Flag);
            vals.Add(r.Roles);
            sb.AppendLine(string.Join(",", vals.Select(Escape)));
        }

        File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true));
        return filePath;
    }

    private static string Escape(string val)
    {
        if (string.IsNullOrEmpty(val)) return "";
        if (val.Contains(',') || val.Contains('"') || val.Contains('\n'))
            return $"\"{val.Replace("\"", "\"\"")}\"";
        return val;
    }
}
