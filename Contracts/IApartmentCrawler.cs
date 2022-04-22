using System.Collections.Generic;
using System.Threading.Tasks;

namespace FrankfurtWohnungsSuchApp.Contracts;

public interface IApartmentCrawler
{
    Task<List<IApartmentData>> GetFlats();
}