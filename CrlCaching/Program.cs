// Program.cs

using System.Net;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

var app = builder.Build();

var cacheOptions = new MemoryCacheEntryOptions()
    .SetAbsoluteExpiration(TimeSpan.FromHours(24));

const string CrlContentType = "application/pkix-crl";

app.MapGet("/{id}", async (string id, IHttpClientFactory factory, IMemoryCache cache) =>
{
    string cacheKey;
    string crlUrl;
    switch (id)
    {
        case "inet":
            cacheKey = "inet_crl_cache";
            crlUrl = "https://ca.inet.co.th/repository/crl/inetca/complete.crl";
            break;
        case "nrca":
            cacheKey = "nrca_crl_cache";
            crlUrl = "http://www.nrca.go.th/crl/THNRCA_arlfile.crl";
            break;
        
        case "tdid":
            cacheKey = "tdid_crl_cache";
            crlUrl =
                "http://www.thaidigitalid.com/tdidcag3crl/certdist?cmd=crl&issuer=CN%3dThai+Digital+ID+CA+G3%2cO%3dThai+Digital+ID+Company+Limited%2cC%3dTH";
            break;
        default:
            return Results.NotFound();
    }

    if (!cache.TryGetValue(cacheKey, out byte[]? cachedCrlBytes))
    {
        var client = factory.CreateClient();
        try
        {
            var response = await client.GetAsync(crlUrl);

            if (response.IsSuccessStatusCode)
            {
                cachedCrlBytes = await response.Content.ReadAsByteArrayAsync();
                cache.Set(cacheKey, cachedCrlBytes, cacheOptions);
                return Results.Stream(new MemoryStream(cachedCrlBytes), contentType: CrlContentType);
            }

            return Results.StatusCode((int)response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return Results.StatusCode((int)HttpStatusCode.ServiceUnavailable); 
        }
        catch (TaskCanceledException)
        {
            return Results.StatusCode((int)HttpStatusCode.GatewayTimeout); 
        }
    }

    if (cachedCrlBytes != null) 
    {
        return Results.Stream(new MemoryStream(cachedCrlBytes), contentType: CrlContentType);
    }

    return Results.StatusCode((int)HttpStatusCode.InternalServerError);
});

app.Run();