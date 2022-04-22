using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FrankfurtWohnungsSuchApp.Contracts;
using Microsoft.Extensions.Logging;

namespace FrankfurtWohnungsSuchApp.Implementation;

public class NassauischHeimCrawler : IApartmentCrawler
{
    private readonly HttpClient _client;
    private readonly ILogger _logger;
    private const string ExposeBaseUrl = "https://www.wohnen-in-der-mitte.de";
    private const string DataUrl = "https://www.wohnen-in-der-mitte.de/mietwohnungen/";


    public NassauischHeimCrawler(HttpClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<List<IApartmentData>> GetFlats()
    {
        var rawHtml = await _client.GetStringAsync(DataUrl);

        return rawHtml
            .Split(@"""card-content""")
            .Skip(1)
            .SkipLast(1)
            .Select(GetCardData)
            .Where(IsInSelectedCity)
            .Where(IsInPriceRange)
            .Where(IsInSizeRange)
            .Select(x => (IApartmentData)x)
            .ToList();
    }


    private static bool IsInSelectedCity(NassauischeRecord x) => x.City is not null;
    private bool IsInPriceRange(NassauischeRecord x) => Parse(x.Prize, out var parsed) && parsed <= Constants.PrizeMax;

    private bool Parse(string text, out double value)
    {
        var isParsed = double.TryParse(text, NumberStyles.Currency, CultureInfo.GetCultureInfo("de-DE"), out value);

        if(!isParsed)_logger.LogWarning("{Text} could not be parsed", text);

        return isParsed;
    }

    private bool IsInSizeRange(NassauischeRecord x) => Parse(x.Size, out var parsed) && parsed >= Constants.SizeMin;

    private static NassauischeRecord GetCardData(string x)
    {
        var price = PriceRegex.Match(x).Value;
        var url = ExposeBaseUrl +  UrlRegex.Match(x).Value;
        var size = SizeRegex.Match(x).Value;
        var title = TitleRegex.Match(x).Value;
        var cityMatch = CityRegex.Match(x);
        var city = cityMatch.Success ? cityMatch.Groups[0].Value : null;

        return new NassauischeRecord(title, size, price, url, title+price, city);
    }

    private static readonly Regex PriceRegex = new(@"[0-9\.]+,[0-9]+(?=\s*€)", RegexOptions.Compiled);
    private static readonly Regex UrlRegex = new(@"(?<=href="").*(?="">)", RegexOptions.Compiled);
    private static readonly Regex SizeRegex = new(@"[0-9]+,[0-9]+(?=\s*m&sup2)", RegexOptions.Compiled);
    private static readonly Regex TitleRegex = new(@"(?<=estate-title"">)[a-zA-Z\s0-9-?!,ßüäö]*", RegexOptions.Compiled);
    private static readonly Regex CityRegex = new(@"(Frankfurt|Offenbach).*(?=<)", RegexOptions.Compiled);

    private record NassauischeRecord(string Name,string Size,string Prize,string Url,string Id,string City) : ApartmentRecord(Name,Size ,Prize,Url,Id);
}