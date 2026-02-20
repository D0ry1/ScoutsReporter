using ClosedXML.Excel;
using ScoutsReporter.Models;

namespace ScoutsReporter.Services;

public static class ExcelExportService
{
    private static readonly Dictionary<string, XLColor> FlagColors = new()
    {
        ["EXPIRED"] = XLColor.FromArgb(220, 53, 69),
        ["ACTION NEEDED"] = XLColor.FromArgb(220, 53, 69),
        ["MISSING"] = XLColor.FromArgb(220, 53, 69),
        ["EXPIRING SOON"] = XLColor.FromArgb(255, 152, 0),
        ["DBS IN PROGRESS"] = XLColor.FromArgb(255, 193, 7),
        ["IN PROGRESS"] = XLColor.FromArgb(255, 193, 7),
        ["NO DISCLOSURE"] = XLColor.FromArgb(108, 117, 125),
        ["NOT IN SYSTEM"] = XLColor.FromArgb(108, 117, 125),
        ["NO PERMITS"] = XLColor.FromArgb(108, 117, 125),
        ["OK"] = XLColor.FromArgb(40, 167, 69),
        ["No expiry"] = XLColor.FromArgb(40, 167, 69),
    };

    private static readonly Dictionary<string, XLColor> FlagBackgrounds = new()
    {
        ["EXPIRED"] = XLColor.FromArgb(255, 220, 220),
        ["ACTION NEEDED"] = XLColor.FromArgb(255, 220, 220),
        ["MISSING"] = XLColor.FromArgb(255, 220, 220),
        ["EXPIRING SOON"] = XLColor.FromArgb(255, 235, 210),
        ["DBS IN PROGRESS"] = XLColor.FromArgb(255, 245, 215),
        ["IN PROGRESS"] = XLColor.FromArgb(255, 245, 215),
    };

    public static void ExportDbsReport(List<DbsReportRow> report, string filePath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("DBS Report");

        var headers = new[]
        {
            "Name", "Mem #", "Email", "Issued", "Expiry", "Warning",
            "Onboarding DBS", "Disclosure Status", "Certificate", "Type",
            "Authority", "Total", "Flag", "Outstanding", "Roles"
        };

        WriteHeaders(ws, headers);

        for (int i = 0; i < report.Count; i++)
        {
            var r = report[i];
            int row = i + 2;
            ws.Cell(row, 1).Value = r.Name;
            ws.Cell(row, 2).Value = r.MembershipNumber;
            ws.Cell(row, 3).Value = r.Email;
            SetDateCell(ws.Cell(row, 4), r.IssuedDate);
            SetDateCell(ws.Cell(row, 5), r.ExpiryDate);
            ws.Cell(row, 6).Value = r.ExpiryWarning;
            ws.Cell(row, 7).Value = r.OnboardingDbs;
            ws.Cell(row, 8).Value = r.DisclosureStatus;
            ws.Cell(row, 9).Value = r.Certificate;
            ws.Cell(row, 10).Value = r.Type;
            ws.Cell(row, 11).Value = r.Authority;
            ws.Cell(row, 12).Value = r.TotalDisclosures;
            SetFlagCell(ws.Cell(row, 13), r.Flag);
            ws.Cell(row, 14).Value = r.OtherOutstanding;
            ws.Cell(row, 15).Value = r.Roles;
        }

        FinalizeSheet(ws);
        wb.SaveAs(filePath);
    }

    public static void ExportTrainingReport(List<TrainingReportRow> report, List<string> sortedTitles, string filePath)
    {
        var expiringTrainings = new HashSet<string> { "First Response", "Safeguarding", "Safety" };

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Training Report");

        // Build headers
        var headers = new List<string> { "Name", "Total Trainings" };
        foreach (var title in sortedTitles)
        {
            headers.Add(title);
            if (expiringTrainings.Contains(title))
                headers.Add($"{title} Warning");
        }
        headers.Add("Flag");
        headers.Add("Roles");

        WriteHeaders(ws, headers.ToArray());

        for (int i = 0; i < report.Count; i++)
        {
            var r = report[i];
            int row = i + 2;
            int col = 1;
            ws.Cell(row, col++).Value = r.Name;
            ws.Cell(row, col++).Value = r.TotalTrainings;

            foreach (var title in sortedTitles)
            {
                var val = r.TrainingColumns.GetValueOrDefault(title, "");
                SetDateCell(ws.Cell(row, col++), val);
                if (expiringTrainings.Contains(title))
                {
                    var warning = r.TrainingColumns.GetValueOrDefault($"{title} Warning", "");
                    ws.Cell(row, col).Value = warning;
                    if (!string.IsNullOrEmpty(warning))
                    {
                        if (warning.Contains("EXPIRED", StringComparison.OrdinalIgnoreCase))
                        {
                            ws.Cell(row, col).Style.Font.FontColor = FlagColors["EXPIRED"];
                            ws.Cell(row, col).Style.Fill.BackgroundColor = FlagBackgrounds["EXPIRED"];
                        }
                        else if (warning.Contains("EXPIRING", StringComparison.OrdinalIgnoreCase))
                        {
                            ws.Cell(row, col).Style.Font.FontColor = FlagColors["EXPIRING SOON"];
                            ws.Cell(row, col).Style.Fill.BackgroundColor = FlagBackgrounds["EXPIRING SOON"];
                        }
                    }
                    col++;
                }
            }

            SetFlagCell(ws.Cell(row, col++), r.Flag);
            ws.Cell(row, col).Value = r.Roles;
        }

        FinalizeSheet(ws);
        wb.SaveAs(filePath);
    }

    public static void ExportPermitsReport(List<PermitReportRow> report, string filePath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Permits Report");

        int maxPermits = report.Count > 0
            ? Math.Max(report.Max(r => r.TotalPermits), 1) : 1;

        var headers = new List<string> { "Name", "Total Permits" };
        for (int p = 1; p <= maxPermits; p++)
        {
            var prefix = $"Permit {p} ";
            headers.AddRange(new[]
            {
                $"{prefix}Name", $"{prefix}Type", $"{prefix}Category",
                $"{prefix}Status", $"{prefix}Issued", $"{prefix}Expiry",
                $"{prefix}Warning", $"{prefix}Restriction", $"{prefix}Assessor"
            });
        }
        headers.Add("Flag");
        headers.Add("Roles");

        WriteHeaders(ws, headers.ToArray());

        for (int i = 0; i < report.Count; i++)
        {
            var r = report[i];
            int row = i + 2;
            int col = 1;
            ws.Cell(row, col++).Value = r.Name;
            ws.Cell(row, col++).Value = r.TotalPermits;

            for (int p = 0; p < maxPermits; p++)
            {
                if (p < r.Permits.Count)
                {
                    var pm = r.Permits[p];
                    ws.Cell(row, col++).Value = pm.Name;
                    ws.Cell(row, col++).Value = pm.Type;
                    ws.Cell(row, col++).Value = pm.Category;
                    ws.Cell(row, col++).Value = pm.Status;
                    SetDateCell(ws.Cell(row, col++), pm.Issued);
                    SetDateCell(ws.Cell(row, col++), pm.Expiry);
                    ws.Cell(row, col).Value = pm.Warning;
                    if (!string.IsNullOrEmpty(pm.Flag) && FlagColors.ContainsKey(pm.Flag))
                        ws.Cell(row, col).Style.Font.FontColor = FlagColors[pm.Flag];
                    col++;
                    ws.Cell(row, col++).Value = pm.Restriction;
                    ws.Cell(row, col++).Value = pm.Assessor;
                }
                else
                {
                    col += 9;
                }
            }

            SetFlagCell(ws.Cell(row, col++), r.Flag);
            ws.Cell(row, col).Value = r.Roles;
        }

        FinalizeSheet(ws);
        wb.SaveAs(filePath);
    }

    private static void WriteHeaders(IXLWorksheet ws, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(27, 94, 32); // #1B5E20
        }
        ws.SheetView.FreezeRows(1);
    }

    private static void SetDateCell(IXLCell cell, string dateStr)
    {
        if (string.IsNullOrEmpty(dateStr))
            return;

        if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt))
        {
            cell.Value = dt;
            cell.Style.DateFormat.Format = "dd/MM/yyyy";
        }
        else
        {
            cell.Value = dateStr;
        }
    }

    private static void SetFlagCell(IXLCell cell, string flag)
    {
        cell.Value = flag;
        cell.Style.Font.Bold = true;

        if (FlagColors.TryGetValue(flag, out var fontColor))
            cell.Style.Font.FontColor = fontColor;

        if (FlagBackgrounds.TryGetValue(flag, out var bgColor))
            cell.Style.Fill.BackgroundColor = bgColor;
    }

    private static void FinalizeSheet(IXLWorksheet ws)
    {
        ws.Columns().AdjustToContents();
    }
}
