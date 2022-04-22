using FrankfurtWohnungsSuchApp.Contracts;

namespace FrankfurtWohnungsSuchApp.Implementation;

public record ApartmentRecord(string Name, string Size, string Prize, string Url, string Id) : IApartmentData;