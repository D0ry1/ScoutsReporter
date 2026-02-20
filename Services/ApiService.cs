using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using ScoutsReporter.Models;

namespace ScoutsReporter.Services;

public class ApiService
{
    private const string ApiBase = "https://tsa-memportal-prod-fun01.azurewebsites.net/api";

    private readonly HttpClient _http;
    private readonly AuthService _auth;

    public ApiService(HttpClient http, AuthService auth)
    {
        _http = http;
        _auth = auth;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, object? jsonBody = null, string? typeHeader = null)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Add("Authorization", $"Bearer {_auth.IdToken}");
        req.Headers.Add("Origin", "https://membership.scouts.org.uk");
        req.Headers.Add("Referer", "https://membership.scouts.org.uk/");
        if (typeHeader != null)
            req.Headers.Add("Type", typeHeader);
        if (jsonBody != null)
            req.Content = JsonContent.Create(jsonBody);
        return req;
    }

    public async Task<JsonElement?> DataExplorerQueryAsync(object body, int retries = 3)
    {
        var url = $"{ApiBase}/DataExplorer/GetResultsAsync";
        JsonElement? result = null;

        for (int attempt = 0; attempt < retries; attempt++)
        {
            var req = CreateRequest(HttpMethod.Post, url, body);
            var resp = await _http.SendAsync(req);
            var json = await resp.Content.ReadAsStringAsync();
            var doc = JsonSerializer.Deserialize<JsonElement>(json);
            result = doc;

            if (doc.ValueKind == JsonValueKind.Object && doc.TryGetProperty("error", out var err))
            {
                if (err.ValueKind == JsonValueKind.Null || (err.ValueKind == JsonValueKind.False))
                    return doc;
                await Task.Delay(3000);
                continue;
            }
            return doc;
        }
        return result;
    }

    public async Task<List<UnitInfo>> FetchUnitsAsync()
    {
        var result = await DataExplorerQueryAsync(new
        {
            contactId = _auth.ContactId,
            distinct = true,
            id = "",
            isDashboardQuery = false,
            name = "",
            query = "",
            selectFields = new[] { "Id", "UnitName" },
            table = "ContactHierarchyUnitsView",
            order = "asc",
            orderBy = "UnitName",
            pageNo = 1,
            pageSize = 50,
        });

        var units = new List<UnitInfo>();
        if (result?.ValueKind == JsonValueKind.Object &&
            result.Value.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in data.EnumerateArray())
            {
                units.Add(new UnitInfo
                {
                    Id = item.GetProp("id"),
                    UnitName = item.GetProp("unitName"),
                });
            }
        }
        return units;
    }

    public async Task<List<TeamInfo>> FetchTeamsAsync(List<UnitInfo> units)
    {
        var teams = new List<TeamInfo>();
        foreach (var u in units)
        {
            var req = CreateRequest(HttpMethod.Post,
                $"{ApiBase}/UnitTeamsAndRolesListingAsync",
                new { unitid = u.Id, isDEFilter = true });
            var resp = await _http.SendAsync(req);
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync();
                var doc = JsonSerializer.Deserialize<JsonElement>(json);
                if (doc.TryGetProperty("teams", out var teamsArr) && teamsArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var t in teamsArr.EnumerateArray())
                    {
                        teams.Add(new TeamInfo
                        {
                            TeamId = t.GetProp("teamId"),
                            TeamName = t.GetProp("teamName"),
                            UnitId = u.Id,
                            UnitName = u.UnitName,
                        });
                    }
                }
            }
            await Task.Delay(300);
        }
        return teams;
    }

    public async Task<Dictionary<string, Member>> FetchTeamMembersAsync(
        List<TeamInfo> teams, IProgress<string>? progress = null)
    {
        var members = new Dictionary<string, Member>();
        foreach (var t in teams)
        {
            var result = await DataExplorerQueryAsync(new
            {
                contactId = _auth.ContactId,
                id = "",
                name = "",
                query = $"teamid='{t.TeamId}' AND unitid ='{t.UnitId}'",
                table = "TeamMembersView",
                selectFields = new[] { "Id", "FullName", "Firstname", "Lastname",
                    "RoleStatusName", "Role", "Unitname", "ContactMembershipId" },
                distinct = true,
                isDashboardQuery = false,
                pageNo = 1,
                pageSize = 100,
            });

            if (result?.ValueKind == JsonValueKind.Object &&
                result.Value.TryGetProperty("data", out var data) &&
                data.ValueKind == JsonValueKind.Array)
            {
                int count = 0;
                foreach (var m in data.EnumerateArray())
                {
                    var cid = m.GetProp("Id");
                    if (string.IsNullOrEmpty(cid)) continue;

                    if (!members.ContainsKey(cid))
                    {
                        members[cid] = new Member
                        {
                            ContactId = cid,
                            FirstName = m.GetProp("Firstname").Trim(),
                            LastName = m.GetProp("Lastname").Trim(),
                            FullName = m.GetProp("FullName").Trim(),
                        };
                    }
                    var roleStr = $"{m.GetProp("Role")} ({m.GetProp("RoleStatusName")}) - {t.TeamName} @ {(string.IsNullOrEmpty(m.GetProp("UnitName")) ? t.UnitName : m.GetProp("UnitName"))}";
                    if (!members[cid].Roles.Contains(roleStr))
                        members[cid].Roles.Add(roleStr);
                    count++;
                }
                progress?.Report($"{t.TeamName} @ {t.UnitName}: {count} members");
            }
            await Task.Delay(300);
        }
        return members;
    }

    // ── DBS-specific APIs ────────────────────────────────────────────

    public async Task<(Dictionary<string, OnboardingInfo> byMn, List<JsonElement> allActions)> FetchOnboardingActionsAsync()
    {
        var result = await DataExplorerQueryAsync(new
        {
            contactId = _auth.ContactId,
            id = "",
            name = "",
            query = "",
            table = "InProgressActionDashboardView",
            selectFields = new[] { "MembershipNumber", "LastName", "PreferredName", "CategoryKey",
                "OnBoardingActionStatus", "Status", "Role", "RoleStatusName",
                "Team", "unitName", "EmailAddress" },
            distinct = true,
            isDashboardQuery = false,
            pageNo = 1,
            pageSize = 500,
        });

        var byMn = new Dictionary<string, OnboardingInfo>();
        var allActions = new List<JsonElement>();

        if (result?.ValueKind == JsonValueKind.Object &&
            result.Value.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.Array)
        {
            foreach (var a in data.EnumerateArray())
            {
                allActions.Add(a);
                var mn = a.GetProp("Membership number");
                if (string.IsNullOrEmpty(mn)) continue;

                if (!byMn.ContainsKey(mn))
                {
                    byMn[mn] = new OnboardingInfo
                    {
                        FirstName = a.GetProp("First name").Trim(),
                        LastName = a.GetProp("Last name").Trim(),
                        Email = a.GetProp("Communication email").Trim(),
                    };
                }
                var cat = a.GetProp("Category key");
                var status = a.GetProp("On boarding action status");
                if (!byMn[mn].Actions.ContainsKey(cat))
                    byMn[mn].Actions[cat] = new List<string>();
                byMn[mn].Actions[cat].Add(status);
                if (cat == "managerDisclosureCheck")
                    byMn[mn].DbsStatuses.Add(status);
                var email = a.GetProp("Communication email").Trim();
                if (!string.IsNullOrEmpty(email) && string.IsNullOrEmpty(byMn[mn].Email))
                    byMn[mn].Email = email;
            }
        }
        return (byMn, allActions);
    }

    public async Task<string> GetSasUrlAsync(string table, string contactId)
    {
        var req = CreateRequest(HttpMethod.Post,
            $"{ApiBase}/GenerateSASTokenAsync",
            new { table, partitionkey = contactId, permissions = "R" },
            typeHeader: "table");
        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return "";
        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonSerializer.Deserialize<JsonElement>(json);
        return doc.GetProp("token");
    }

    public async Task<List<JsonElement>> FetchTableRecordsAsync(string sasUrl, string tableName)
    {
        var queryUrl = sasUrl.Replace($"/{tableName}?", $"/{tableName}()?");
        var req = new HttpRequestMessage(HttpMethod.Get, queryUrl);
        req.Headers.Add("Accept", "application/json;odata=minimalmetadata");
        req.Headers.Add("DataServiceVersion", "3.0");
        req.Headers.Add("x-ms-version", "2019-02-02");
        req.Headers.Add("Origin", "https://membership.scouts.org.uk");
        var resp = await _http.SendAsync(req);
        if (resp.IsSuccessStatusCode)
        {
            var json = await resp.Content.ReadAsStringAsync();
            var doc = JsonSerializer.Deserialize<JsonElement>(json);
            if (doc.TryGetProperty("value", out var val) && val.ValueKind == JsonValueKind.Array)
                return val.EnumerateArray().ToList();
        }
        return new();
    }

    // ── Training-specific APIs ────────────────────────────────────────

    public async Task<List<JsonElement>> FetchLmsDetailsAsync(string contactId)
    {
        var req = CreateRequest(HttpMethod.Post,
            $"{ApiBase}/GetLmsDetailsAsync",
            new { contactid = contactId });
        var resp = await _http.SendAsync(req);
        if (resp.IsSuccessStatusCode)
        {
            var json = await resp.Content.ReadAsStringAsync();
            var doc = JsonSerializer.Deserialize<JsonElement>(json);
            if (doc.ValueKind == JsonValueKind.Array)
                return doc.EnumerateArray().ToList();
        }
        return new();
    }
}

public static class JsonElementExtensions
{
    public static string GetProp(this JsonElement el, string name)
    {
        if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var val))
        {
            if (val.ValueKind == JsonValueKind.String) return val.GetString() ?? "";
            if (val.ValueKind == JsonValueKind.Number) return val.ToString();
            if (val.ValueKind == JsonValueKind.Null) return "";
            return val.ToString();
        }
        return "";
    }
}
