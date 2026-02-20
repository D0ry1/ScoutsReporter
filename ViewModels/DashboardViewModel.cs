using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScoutsReporter.Models;

namespace ScoutsReporter.ViewModels;

public class FlagGroup
{
    public string FlagName { get; set; } = "";
    public int Count { get; set; }
    public SolidColorBrush FlagBrush { get; set; } = new(Colors.Gray);
    public string Names { get; set; } = "";
}

public class TrainingBreakdown
{
    public string Title { get; set; } = "";
    public double CompliancePercent { get; set; }
    public int OkCount { get; set; }
    public int ExpiredCount { get; set; }
    public int ExpiringSoonCount { get; set; }
    public int MissingCount { get; set; }
    public string ExpiredNames { get; set; } = "";
    public string ExpiringSoonNames { get; set; } = "";
    public string MissingNames { get; set; } = "";
}

public class SectionBreakdown
{
    public string TeamName { get; set; } = "";
    public int MemberCount { get; set; }
    public double DbsPercent { get; set; }
    public double SafeguardingPercent { get; set; }
    public double FirstResponsePercent { get; set; }
}

public partial class UnitBreakdown : ObservableObject
{
    public string UnitName { get; set; } = "";
    public int MemberCount { get; set; }
    public double DbsPercent { get; set; }
    public double SafeguardingPercent { get; set; }
    public double FirstResponsePercent { get; set; }
    public bool HasSections { get; set; }
    public List<SectionBreakdown> Sections { get; set; } = new();

    // Per-unit DBS detail
    public List<FlagGroup> DbsFlagTiles { get; set; } = new();
    public List<FlagGroup> DbsAttentionGroups { get; set; } = new();

    // Per-unit training detail
    public List<TrainingBreakdown> SafeguardingBreakdowns { get; set; } = new();
    public List<FlagGroup> SafeguardingAttentionGroups { get; set; } = new();
    public List<TrainingBreakdown> FirstResponseBreakdowns { get; set; } = new();
    public List<FlagGroup> FirstResponseAttentionGroups { get; set; } = new();

    [ObservableProperty]
    private bool _isExpanded;

    [RelayCommand]
    private void ToggleExpand() => IsExpanded = !IsExpanded;
}

public partial class DashboardViewModel : ObservableObject
{
    private readonly DbsReportViewModel _dbsReport;
    private readonly TrainingReportViewModel _trainingReport;
    private readonly MainViewModel _main;

    // Overall
    [ObservableProperty] private double _overallCompliancePercent;
    [ObservableProperty] private double _dbsCompliancePercent;
    [ObservableProperty] private double _trainingCompliancePercent;
    [ObservableProperty] private int _totalMembers;
    [ObservableProperty] private bool _hasData;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _statusText = "Click 'Run Dashboard' to generate the compliance overview.";
    [ObservableProperty] private string _progressText = "";

    // DBS
    [ObservableProperty] private int _dbsOkCount;
    [ObservableProperty] private int _dbsTotalCount;
    public ObservableCollection<FlagGroup> DbsFlagTiles { get; } = new();
    public ObservableCollection<FlagGroup> DbsAttentionGroups { get; } = new();

    // Training — Safety & Safeguarding (compliance-affecting)
    [ObservableProperty] private double _safeguardingCompliancePercent;
    [ObservableProperty] private int _safeguardingOkCount;
    [ObservableProperty] private int _safeguardingTotalCount;
    public ObservableCollection<TrainingBreakdown> SafeguardingBreakdowns { get; } = new();
    public ObservableCollection<FlagGroup> SafeguardingAttentionGroups { get; } = new();

    // Training — First Response (tracked separately, not compliance-affecting)
    [ObservableProperty] private double _firstResponseCompliancePercent;
    [ObservableProperty] private int _firstResponseOkCount;
    [ObservableProperty] private int _firstResponseTotalCount;
    public ObservableCollection<TrainingBreakdown> FirstResponseBreakdowns { get; } = new();
    public ObservableCollection<FlagGroup> FirstResponseAttentionGroups { get; } = new();

    // Per-unit
    [ObservableProperty] private bool _showPerUnit;
    public ObservableCollection<UnitBreakdown> UnitBreakdowns { get; } = new();

    public DashboardViewModel(DbsReportViewModel dbsReport, TrainingReportViewModel trainingReport, MainViewModel main)
    {
        _dbsReport = dbsReport;
        _trainingReport = trainingReport;
        _main = main;
    }

    [RelayCommand]
    private async Task RunDashboardAsync()
    {
        if (!_main.IsAuthenticated)
        {
            StatusText = "Please authenticate first.";
            return;
        }

        IsRunning = true;
        HasData = false;
        StatusText = "Running reports...";

        try
        {
            ProgressText = "Running DBS report...";
            await _dbsReport.RunReportCommand.ExecuteAsync(null);

            ProgressText = "Running Training report...";
            await _trainingReport.RunReportCommand.ExecuteAsync(null);

            ProgressText = "Aggregating results...";
            AggregateResults();

            StatusText = $"Dashboard complete — {TotalMembers} members analysed.";
            ProgressText = "";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
        }
    }

    private void AggregateResults()
    {
        // ── DBS ──────────────────────────────────────────────
        var dbsRows = _dbsReport.AllRows;
        DbsTotalCount = dbsRows.Count;
        DbsOkCount = dbsRows.Count(r => r.Flag is "OK" or "EXPIRING SOON");
        DbsCompliancePercent = DbsTotalCount > 0
            ? Math.Round((double)DbsOkCount / DbsTotalCount * 100, 1) : 0;

        DbsFlagTiles.Clear();
        DbsAttentionGroups.Clear();

        var dbsGrouped = dbsRows
            .GroupBy(r => r.Flag)
            .OrderByDescending(g => g.Key == "OK")
            .ThenBy(g => g.Key);

        foreach (var group in dbsGrouped)
        {
            var brush = GetDbsFlagBrush(group.Key);
            DbsFlagTiles.Add(new FlagGroup
            {
                FlagName = group.Key,
                Count = group.Count(),
                FlagBrush = brush,
            });

            if (group.Key != "OK")
            {
                DbsAttentionGroups.Add(new FlagGroup
                {
                    FlagName = group.Key,
                    Count = group.Count(),
                    FlagBrush = brush,
                    Names = string.Join(", ", group.Select(r => r.Name)),
                });
            }
        }

        // ── Training ─────────────────────────────────────────
        var trainingRows = _trainingReport.ReportRowsReadOnly;
        var safeguardingTrainings = new[] { "Safety", "Safeguarding" };
        var firstResponseTrainings = new[] { "First Response" };

        // Safety & Safeguarding (compliance-affecting)
        SafeguardingBreakdowns.Clear();
        SafeguardingAttentionGroups.Clear();
        SafeguardingTotalCount = trainingRows.Count;

        foreach (var training in safeguardingTrainings)
        {
            var breakdown = BuildTrainingBreakdown(training, trainingRows);
            SafeguardingBreakdowns.Add(breakdown);
            AddAttentionGroups(SafeguardingAttentionGroups, training, breakdown);
        }

        var sgCompliant = trainingRows.Count(row =>
            safeguardingTrainings.All(t => ClassifyTraining(row, t) is TrainingStatus.Ok or TrainingStatus.ExpiringSoon));
        SafeguardingOkCount = sgCompliant;
        SafeguardingCompliancePercent = SafeguardingTotalCount > 0
            ? Math.Round((double)sgCompliant / SafeguardingTotalCount * 100, 1) : 0;

        // First Response (tracked separately, not compliance-affecting)
        FirstResponseBreakdowns.Clear();
        FirstResponseAttentionGroups.Clear();
        FirstResponseTotalCount = trainingRows.Count;

        foreach (var training in firstResponseTrainings)
        {
            var breakdown = BuildTrainingBreakdown(training, trainingRows);
            FirstResponseBreakdowns.Add(breakdown);
            AddAttentionGroups(FirstResponseAttentionGroups, training, breakdown);
        }

        var frCompliant = trainingRows.Count(row =>
            firstResponseTrainings.All(t => ClassifyTraining(row, t) is TrainingStatus.Ok or TrainingStatus.ExpiringSoon));
        FirstResponseOkCount = frCompliant;
        FirstResponseCompliancePercent = FirstResponseTotalCount > 0
            ? Math.Round((double)frCompliant / FirstResponseTotalCount * 100, 1) : 0;

        // Training compliance for overall = Safety & Safeguarding only
        TrainingCompliancePercent = SafeguardingCompliancePercent;

        // ── Overall ──────────────────────────────────────────
        TotalMembers = Math.Max(DbsTotalCount, trainingRows.Count);
        OverallCompliancePercent = Math.Round((DbsCompliancePercent + TrainingCompliancePercent) / 2, 1);

        // ── Per-unit breakdown ───────────────────────────────
        AggregatePerUnit(dbsRows, trainingRows, safeguardingTrainings);

        HasData = true;
    }

    private static void PopulateUnitDetail(
        UnitBreakdown breakdown,
        List<DbsReportRow> dbsRows,
        List<TrainingReportRow> trainingRows)
    {
        // DBS flag tiles + attention groups
        var dbsGrouped = dbsRows
            .GroupBy(r => r.Flag)
            .OrderByDescending(g => g.Key == "OK")
            .ThenBy(g => g.Key);

        foreach (var group in dbsGrouped)
        {
            var brush = GetDbsFlagBrush(group.Key);
            breakdown.DbsFlagTiles.Add(new FlagGroup
            {
                FlagName = group.Key,
                Count = group.Count(),
                FlagBrush = brush,
            });
            if (group.Key != "OK")
            {
                breakdown.DbsAttentionGroups.Add(new FlagGroup
                {
                    FlagName = group.Key,
                    Count = group.Count(),
                    FlagBrush = brush,
                    Names = string.Join(", ", group.Select(r => r.Name)),
                });
            }
        }

        // Safety & Safeguarding
        foreach (var training in new[] { "Safety", "Safeguarding" })
        {
            var tb = BuildTrainingBreakdown(training, trainingRows);
            breakdown.SafeguardingBreakdowns.Add(tb);
            AddAttentionGroupsToList(breakdown.SafeguardingAttentionGroups, training, tb);
        }

        // First Response
        var fr = BuildTrainingBreakdown("First Response", trainingRows);
        breakdown.FirstResponseBreakdowns.Add(fr);
        AddAttentionGroupsToList(breakdown.FirstResponseAttentionGroups, "First Response", fr);
    }

    private static void AddAttentionGroupsToList(List<FlagGroup> target, string training, TrainingBreakdown breakdown)
    {
        if (breakdown.ExpiredCount > 0)
            target.Add(new FlagGroup
            {
                FlagName = $"{training} — Expired",
                Count = breakdown.ExpiredCount,
                FlagBrush = FindBrush("StatusDangerBrush", Color.FromRgb(0xe2, 0x2e, 0x12)),
                Names = breakdown.ExpiredNames,
            });
        if (breakdown.ExpiringSoonCount > 0)
            target.Add(new FlagGroup
            {
                FlagName = $"{training} — Expiring Soon",
                Count = breakdown.ExpiringSoonCount,
                FlagBrush = FindBrush("StatusWarningBrush", Color.FromRgb(0xFF, 0x98, 0x00)),
                Names = breakdown.ExpiringSoonNames,
            });
        if (breakdown.MissingCount > 0)
            target.Add(new FlagGroup
            {
                FlagName = $"{training} — Missing",
                Count = breakdown.MissingCount,
                FlagBrush = FindBrush("StatusNavyBrush", Color.FromRgb(0x00, 0x39, 0x82)),
                Names = breakdown.MissingNames,
            });
    }

    private static void AddAttentionGroups(ObservableCollection<FlagGroup> target, string training, TrainingBreakdown breakdown)
    {
        if (breakdown.ExpiredCount > 0)
        {
            target.Add(new FlagGroup
            {
                FlagName = $"{training} — Expired",
                Count = breakdown.ExpiredCount,
                FlagBrush = FindBrush("StatusDangerBrush", Color.FromRgb(0xe2, 0x2e, 0x12)),
                Names = breakdown.ExpiredNames,
            });
        }
        if (breakdown.ExpiringSoonCount > 0)
        {
            target.Add(new FlagGroup
            {
                FlagName = $"{training} — Expiring Soon",
                Count = breakdown.ExpiringSoonCount,
                FlagBrush = FindBrush("StatusWarningBrush", Color.FromRgb(0xFF, 0x98, 0x00)),
                Names = breakdown.ExpiringSoonNames,
            });
        }
        if (breakdown.MissingCount > 0)
        {
            target.Add(new FlagGroup
            {
                FlagName = $"{training} — Missing",
                Count = breakdown.MissingCount,
                FlagBrush = FindBrush("StatusNavyBrush", Color.FromRgb(0x00, 0x39, 0x82)),
                Names = breakdown.MissingNames,
            });
        }
    }

    private void AggregatePerUnit(
        IReadOnlyList<DbsReportRow> dbsRows,
        IReadOnlyList<TrainingReportRow> trainingRows,
        string[] mandatoryTrainings)
    {
        UnitBreakdowns.Clear();

        var members = _main.Cache.Members;
        if (members == null || members.Count == 0)
        {
            ShowPerUnit = false;
            return;
        }

        // Build name → unit names and name → (unit → teams) lookups
        var nameToUnits = new Dictionary<string, HashSet<string>>();
        var nameToUnitTeams = new Dictionary<string, Dictionary<string, HashSet<string>>>();
        foreach (var m in members.Values)
        {
            if (string.IsNullOrEmpty(m.FullName)) continue;
            if (m.UnitNames.Count > 0)
                nameToUnits[m.FullName] = m.UnitNames;
            if (m.UnitTeams.Count > 0)
                nameToUnitTeams[m.FullName] = m.UnitTeams;
        }

        // Collect all unit names
        var allUnits = nameToUnits.Values
            .SelectMany(u => u)
            .Distinct()
            .OrderBy(u => u)
            .ToList();

        // Group DBS rows by unit
        var dbsByUnit = new Dictionary<string, List<DbsReportRow>>();
        foreach (var unit in allUnits)
            dbsByUnit[unit] = new();
        foreach (var row in dbsRows)
        {
            if (nameToUnits.TryGetValue(row.Name, out var units))
                foreach (var unit in units)
                    dbsByUnit[unit].Add(row);
        }

        // Group training rows by unit
        var trainingByUnit = new Dictionary<string, List<TrainingReportRow>>();
        foreach (var unit in allUnits)
            trainingByUnit[unit] = new();
        foreach (var row in trainingRows)
        {
            if (nameToUnits.TryGetValue(row.Name, out var units))
                foreach (var unit in units)
                    trainingByUnit[unit].Add(row);
        }

        // Build team-level lookups for DBS and training rows
        var teamsPerUnit = new Dictionary<string, HashSet<string>>();
        foreach (var ut in nameToUnitTeams.Values)
            foreach (var (unitName, teams) in ut)
            {
                if (!teamsPerUnit.ContainsKey(unitName))
                    teamsPerUnit[unitName] = new();
                foreach (var team in teams)
                    teamsPerUnit[unitName].Add(team);
            }

        var dbsByUnitTeam = new Dictionary<(string, string), List<DbsReportRow>>();
        foreach (var row in dbsRows)
        {
            if (nameToUnitTeams.TryGetValue(row.Name, out var ut))
                foreach (var (unitName, teams) in ut)
                    foreach (var team in teams)
                    {
                        var key = (unitName, team);
                        if (!dbsByUnitTeam.ContainsKey(key)) dbsByUnitTeam[key] = new();
                        dbsByUnitTeam[key].Add(row);
                    }
        }

        var trainingByUnitTeam = new Dictionary<(string, string), List<TrainingReportRow>>();
        foreach (var row in trainingRows)
        {
            if (nameToUnitTeams.TryGetValue(row.Name, out var ut))
                foreach (var (unitName, teams) in ut)
                    foreach (var team in teams)
                    {
                        var key = (unitName, team);
                        if (!trainingByUnitTeam.ContainsKey(key)) trainingByUnitTeam[key] = new();
                        trainingByUnitTeam[key].Add(row);
                    }
        }

        // Group related units under their parent Scout Group
        var unitGroups = GroupRelatedUnits(allUnits);

        // Always show per-unit breakdown — detail cards live inside each unit
        ShowPerUnit = true;

        var sgTrainings = new[] { "Safety", "Safeguarding" };
        var frTrainings = new[] { "First Response" };

        foreach (var group in unitGroups)
        {
            // Aggregate across all units in the group, deduplicated by member name
            var groupDbs = group.SectionUnits
                .SelectMany(u => dbsByUnit.GetValueOrDefault(u, new()))
                .GroupBy(r => r.Name).Select(g => g.First()).ToList();
            var groupTraining = group.SectionUnits
                .SelectMany(u => trainingByUnit.GetValueOrDefault(u, new()))
                .GroupBy(r => r.Name).Select(g => g.First()).ToList();

            var memberCount = Math.Max(groupDbs.Count, groupTraining.Count);

            var breakdown = new UnitBreakdown
            {
                UnitName = group.GroupName,
                MemberCount = memberCount,
                DbsPercent = CalcDbsPercent(groupDbs),
                SafeguardingPercent = CalcTrainingPercent(groupTraining, sgTrainings),
                FirstResponsePercent = CalcTrainingPercent(groupTraining, frTrainings),
            };

            // Populate DBS + training detail for this group
            PopulateUnitDetail(breakdown, groupDbs, groupTraining);

            if (group.SectionUnits.Count > 1)
            {
                // Multi-unit group: sections are the individual units
                breakdown.HasSections = true;
                foreach (var unit in group.SectionUnits)
                {
                    var unitDbs = dbsByUnit.GetValueOrDefault(unit, new());
                    var unitTraining = trainingByUnit.GetValueOrDefault(unit, new());
                    var sectionName = StripGroupPrefix(unit, group.Prefix);

                    breakdown.Sections.Add(new SectionBreakdown
                    {
                        TeamName = sectionName,
                        MemberCount = Math.Max(unitDbs.Count, unitTraining.Count),
                        DbsPercent = CalcDbsPercent(unitDbs),
                        SafeguardingPercent = CalcTrainingPercent(unitTraining, sgTrainings),
                        FirstResponsePercent = CalcTrainingPercent(unitTraining, frTrainings),
                    });
                }
            }
            else if (group.SectionUnits.Count == 1
                     && teamsPerUnit.TryGetValue(group.SectionUnits[0], out var teams)
                     && teams.Count > 1)
            {
                // Single-unit group: fall back to teams as sections
                breakdown.HasSections = true;
                var unitName = group.SectionUnits[0];
                foreach (var team in teams.OrderBy(t => t))
                {
                    var teamDbs = dbsByUnitTeam.GetValueOrDefault((unitName, team), new());
                    var teamTraining = trainingByUnitTeam.GetValueOrDefault((unitName, team), new());

                    breakdown.Sections.Add(new SectionBreakdown
                    {
                        TeamName = team,
                        MemberCount = Math.Max(teamDbs.Count, teamTraining.Count),
                        DbsPercent = CalcDbsPercent(teamDbs),
                        SafeguardingPercent = CalcTrainingPercent(teamTraining, sgTrainings),
                        FirstResponsePercent = CalcTrainingPercent(teamTraining, frTrainings),
                    });
                }
            }

            UnitBreakdowns.Add(breakdown);
        }
    }

    private record UnitGroup(string GroupName, string Prefix, List<string> SectionUnits);

    private static List<UnitGroup> GroupRelatedUnits(List<string> allUnits)
    {
        var groups = new List<UnitGroup>();
        var assigned = new HashSet<string>();

        // Sort shortest-first so parent names come before child names
        var sorted = allUnits.OrderBy(u => u.Length).ThenBy(u => u).ToList();

        foreach (var candidate in sorted)
        {
            if (assigned.Contains(candidate)) continue;

            // Find all unassigned units that start with this candidate's name
            // Use candidate + " - " as the prefix to avoid partial word matches
            var children = sorted
                .Where(u => !assigned.Contains(u)
                    && u != candidate
                    && u.StartsWith(candidate, StringComparison.OrdinalIgnoreCase)
                    && u.Length > candidate.Length
                    && (u[candidate.Length] == ' ' || u[candidate.Length] == '-'))
                .ToList();

            if (children.Count > 0)
            {
                // This candidate is a parent group — collect it plus its children
                var sectionUnits = new List<string> { candidate };
                sectionUnits.AddRange(children);
                sectionUnits.Sort(StringComparer.OrdinalIgnoreCase);

                groups.Add(new UnitGroup(candidate, candidate, sectionUnits));
                foreach (var s in sectionUnits) assigned.Add(s);
            }
        }

        // Standalone units that didn't match any group
        foreach (var unit in allUnits.Where(u => !assigned.Contains(u)))
        {
            groups.Add(new UnitGroup(unit, "", new List<string> { unit }));
            assigned.Add(unit);
        }

        return groups.OrderBy(g => g.GroupName).ToList();
    }

    private static string StripGroupPrefix(string unitName, string prefix)
    {
        if (string.IsNullOrEmpty(prefix)
            || !unitName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return unitName;

        var stripped = unitName[prefix.Length..].TrimStart(' ', '-').TrimStart();
        return string.IsNullOrEmpty(stripped) ? unitName : stripped;
    }

    private static double CalcDbsPercent(List<DbsReportRow> rows)
    {
        if (rows.Count == 0) return 0;
        var ok = rows.Count(r => r.Flag is "OK" or "EXPIRING SOON");
        return Math.Round((double)ok / rows.Count * 100, 1);
    }

    private static double CalcTrainingPercent(List<TrainingReportRow> rows, string[] trainings)
    {
        if (rows.Count == 0) return 0;
        var ok = rows.Count(row =>
            trainings.All(t => ClassifyTraining(row, t) is TrainingStatus.Ok or TrainingStatus.ExpiringSoon));
        return Math.Round((double)ok / rows.Count * 100, 1);
    }

    private static TrainingBreakdown BuildTrainingBreakdown(string title, IReadOnlyList<TrainingReportRow> rows)
    {
        var ok = new List<string>();
        var expired = new List<string>();
        var expiring = new List<string>();
        var missing = new List<string>();

        foreach (var row in rows)
        {
            switch (ClassifyTraining(row, title))
            {
                case TrainingStatus.Ok: ok.Add(row.Name); break;
                case TrainingStatus.Expired: expired.Add(row.Name); break;
                case TrainingStatus.ExpiringSoon: expiring.Add(row.Name); break;
                case TrainingStatus.Missing: missing.Add(row.Name); break;
            }
        }

        var total = rows.Count;
        return new TrainingBreakdown
        {
            Title = title,
            CompliancePercent = total > 0 ? Math.Round((double)(ok.Count + expiring.Count) / total * 100, 1) : 0,
            OkCount = ok.Count,
            ExpiredCount = expired.Count,
            ExpiringSoonCount = expiring.Count,
            MissingCount = missing.Count,
            ExpiredNames = string.Join(", ", expired),
            ExpiringSoonNames = string.Join(", ", expiring),
            MissingNames = string.Join(", ", missing),
        };
    }

    private enum TrainingStatus { Ok, Expired, ExpiringSoon, Missing }

    private static TrainingStatus ClassifyTraining(TrainingReportRow row, string title)
    {
        var mainVal = row.TrainingColumns.GetValueOrDefault(title, "");
        var warnVal = row.TrainingColumns.GetValueOrDefault($"{title} Warning", "");

        if (string.Equals(mainVal, "MISSING", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(mainVal))
            return TrainingStatus.Missing;

        if (warnVal.Contains("Expired", StringComparison.OrdinalIgnoreCase))
            return TrainingStatus.Expired;

        if (warnVal.Contains("days remaining", StringComparison.OrdinalIgnoreCase))
        {
            var match = Regex.Match(warnVal, @"(\d+)\s*days remaining");
            if (match.Success && int.Parse(match.Groups[1].Value) < 90)
                return TrainingStatus.ExpiringSoon;
        }

        return TrainingStatus.Ok;
    }

    private static SolidColorBrush GetDbsFlagBrush(string flag)
    {
        return flag switch
        {
            "OK" => FindBrush("StatusOkBrush", Color.FromRgb(0x23, 0xa9, 0x50)),
            "EXPIRED" or "ACTION NEEDED" => FindBrush("StatusDangerBrush", Color.FromRgb(0xe2, 0x2e, 0x12)),
            "EXPIRING SOON" or "DBS IN PROGRESS" or "IN PROGRESS"
                => FindBrush("StatusWarningBrush", Color.FromRgb(0xFF, 0x98, 0x00)),
            "NO DISCLOSURE" or "NOT IN SYSTEM" => FindBrush("StatusNavyBrush", Color.FromRgb(0x00, 0x39, 0x82)),
            _ => FindBrush("MutedTextBrush", Color.FromRgb(0x75, 0x75, 0x75)),
        };
    }

    private static SolidColorBrush FindBrush(string key, Color fallback)
    {
        if (Application.Current?.TryFindResource(key) is SolidColorBrush brush)
            return brush;
        return new SolidColorBrush(fallback);
    }

    public void ClearDashboard()
    {
        DbsFlagTiles.Clear();
        DbsAttentionGroups.Clear();
        SafeguardingBreakdowns.Clear();
        SafeguardingAttentionGroups.Clear();
        FirstResponseBreakdowns.Clear();
        FirstResponseAttentionGroups.Clear();
        UnitBreakdowns.Clear();
        OverallCompliancePercent = 0;
        DbsCompliancePercent = 0;
        TrainingCompliancePercent = 0;
        SafeguardingCompliancePercent = 0;
        SafeguardingOkCount = 0;
        SafeguardingTotalCount = 0;
        FirstResponseCompliancePercent = 0;
        FirstResponseOkCount = 0;
        FirstResponseTotalCount = 0;
        DbsOkCount = 0;
        DbsTotalCount = 0;
        TotalMembers = 0;
        HasData = false;
        ShowPerUnit = false;
        StatusText = "Click 'Run Dashboard' to generate the compliance overview.";
        ProgressText = "";
    }
}
