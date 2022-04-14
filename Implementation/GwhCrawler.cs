using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FrankfurtWohnungsSuchApp
{
    public class GwhCrawler : IApartmentCrawler
    {
        private readonly HttpClient _client;
        private const string ExposeBaseUrl = "https://www.gwh.de/mieten/objekt/";
        private const string DataUrl = "https://www.gwh.de/api/v1/openimmo/mapobjects";

        public GwhCrawler(HttpClient client)
        {
            _client = client;
        }

        public async Task<List<IApartmentData>> GetFlats()
        {
            MultipartFormDataContent formData = CreateRequest();

            var response = await _client.PostAsync(DataUrl, formData);
            
            var result = await response.Content.ReadAsStringAsync();

            return JsonConvert
                .DeserializeObject<List<GwhResult>>(result)
                .Select(ToFlatData)
                .ToList();
        }

        private static MultipartFormDataContent CreateRequest()
        {
            var formdata = new MultipartFormDataContent();
            AddContent("city", "Frankfurt am Main");
            AddContent("nutzungsart", "wohnung");
            AddContent("numberOfRoomsMin", "0");
            AddContent("numberOfRoomsMax", "0");
            AddContent("priceMin", "0");
            AddContent("priceMax", Constants.PrizeMax.ToString());
            AddContent("livingAreaMin", Constants.SizeMin.ToString());
            AddContent("livingAreaMax", "120");
            return formdata;

            void AddContent(string key, string value)
            {
                formdata.Add(new StringContent(value), $"tx_openimmotypo3_map[{key}]");
            }
        }

        private static IApartmentData ToFlatData(GwhResult result) => new ApartmentRecord(
            result.objekttitel,
            result.wohnen,
            result.gesamtmietebrutto,
            DetailExposeUrl(result.openimmo_obid),
            result.openimmo_obid);
        private static string DetailExposeUrl(string id) => ExposeBaseUrl + id;
        private record GwhResult(string openimmo_obid, string objekttitel, string gesamtmietebrutto, string wohnen);
    }
}
