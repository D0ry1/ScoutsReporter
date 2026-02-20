namespace ScoutsReporter.Models;

public class TrainingRecord
{
    public string Title { get; set; } = "";
    public string CurrentLevel { get; set; } = "";
    public string ExpiryDate { get; set; } = "";
}

public class TrainingReportRow
{
    public string Name { get; set; } = "";
    public string Roles { get; set; } = "";
    public int TotalTrainings { get; set; }
    public string Flag { get; set; } = "";

    // Dynamic training columns stored as key-value pairs
    public Dictionary<string, string> TrainingColumns { get; set; } = new();
}
