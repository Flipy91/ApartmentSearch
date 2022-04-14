using System.Collections.Generic;
using System.Threading.Tasks;

namespace FrankfurtWohnungsSuchApp
{
    public interface IApartmentCrawler
    {
        Task<List<IApartmentData>> GetFlats();
    }
}
