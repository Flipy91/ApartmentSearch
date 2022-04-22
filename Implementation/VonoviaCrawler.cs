using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FrankfurtWohnungsSuchApp.Contracts;

namespace FrankfurtWohnungsSuchApp.Implementation;

public class VonoviaCrawler : IApartmentCrawler
{
    private readonly HttpClient _client;
    private const string VonoviaExposeBaseUrl = "https://www.vonovia.de/de-de/immobiliensuche/";
    private static readonly string DataUrl = $"https://www.wohnraumkarte.de/Api/getImmoList?limit={Constants.PrizeMax}&city=Frankfurt+am+Main,+Deutschland&rentType=miete&sizeMin={Constants.SizeMin}&minRooms=2&dachgeschoss=0&erdgeschoss=0&sofortfrei=egal&balcony=egal&disabilityAccess=egal&subsidizedHousingPermit=egal&geoLocation=1&perimeter={Constants.Radius}";
                
    public VonoviaCrawler(HttpClient client)
    {
        _client = client;
    }

    public async Task<List<IApartmentData>> GetFlats()
    {
        var data = await _client.GetFromJsonAsync<VonoviaData>(DataUrl);
        return data.results.Select(ToFlatData).ToList();
    }

    private static IApartmentData ToFlatData(VonoviaResult result) => new ApartmentRecord(result.slug,result.groesse,result.preis,DetailExposeUrl(result.slug,result.wrk_id), result.wrk_id);
    private static string DetailExposeUrl(string title,string id) => VonoviaExposeBaseUrl + title + "-" + id;
    private record VonoviaData(List<VonoviaResult> results);
    private record VonoviaResult(string wrk_id, string slug, string preis, string groesse);
}