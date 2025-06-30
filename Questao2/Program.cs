using Newtonsoft.Json;
using System.Text.Json;

public class Program
{
    public static async Task Main()
    {
        string teamName = "Paris Saint-Germain";
        int year = 2013;
        int totalGoals = await getTotalScoredGoals(teamName, year);

        Console.WriteLine("Team "+ teamName +" scored "+ totalGoals.ToString() + " goals in "+ year);

        teamName = "Chelsea";
        year = 2014;
        totalGoals = await getTotalScoredGoals(teamName, year);

        Console.WriteLine("Team " + teamName + " scored " + totalGoals.ToString() + " goals in " + year);

        // Output expected:
        // Team Paris Saint - Germain scored 109 goals in 2013
        // Team Chelsea scored 92 goals in 2014
    }

public static async Task<int> getTotalScoredGoals(string team, int year)
    {
        int totalGoals = 0;
        totalGoals += await GetGoalsByTeamRole(team, year, "team1", "team1goals");
        totalGoals += await GetGoalsByTeamRole(team, year, "team2", "team2goals");
        return totalGoals;
    }

    private static async Task<int> GetGoalsByTeamRole(string team, int year, string teamParam, string goalParam)
    {
        using var client = new HttpClient();
        int page = 1;
        int totalPages = 1;
        int goals = 0;

        while (page <= totalPages)
        {
            string url = $"https://jsonmock.hackerrank.com/api/football_matches?year={year}&{teamParam}={Uri.EscapeDataString(team)}&page={page}";
            var response = await client.GetStringAsync(url);

            using JsonDocument doc = JsonDocument.Parse(response);
            var root = doc.RootElement;
            totalPages = root.GetProperty("total_pages").GetInt32();

            foreach (var match in root.GetProperty("data").EnumerateArray())
            {
                string goalStr = match.GetProperty(goalParam).GetString();
                if (int.TryParse(goalStr, out int g))
                    goals += g;
            }

            page++;
        }

        return goals;
    }
}