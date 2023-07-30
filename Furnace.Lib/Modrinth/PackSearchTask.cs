using Furnace.Lib.Web;
using Furnace.Modrinth.Data.SearchQueryResult;

namespace Furnace.Lib.Modrinth;

public class PackSearchTask : Runnable.Runnable
{
    
    private const string ModrinthSearchUri = "https://api.modrinth.com/v2/search?query={0}";

    private readonly string _query;

    public PackSearchTask(string query)
    {
        _query = query;
    }

    public override async Task<IEnumerable<Hit>> RunAsync(CancellationToken ct)
    {
        var queryResult = await WebService.GetJson(
            new Uri(string.Format(ModrinthSearchUri, _query)),
            SearchQueryResult.FromJson,
            ct
        );

        return queryResult.Hits.Take(10);
    }

    public override string Tag => $"Modrinth search query";
    
}