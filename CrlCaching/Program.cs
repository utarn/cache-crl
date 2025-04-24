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

app.MapGet("/inet", async (IHttpClientFactory factory, IMemoryCache cache) =>
{
    const string cacheKey = "inet_crl_cache";
    const string crlUrl = "https://ca.inet.co.th/repository/crl/inetca/complete.crl";

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

app.MapGet("/nrca", async (IHttpClientFactory factory, IMemoryCache cache) => 
{
    const string cacheKey = "nrca_crl_cache";
    const string crlUrl = "http://www.nrca.go.th/crl/THNRCA_arlfile.crl"; 

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
            return Results.StatusCode((int)HttpStatusCode.GatewayTimeout); // Or ServiceUnavailable
        }
    }

    if (cachedCrlBytes != null)
    {
        return Results.Stream(new MemoryStream(cachedCrlBytes), contentType: CrlContentType);
    }

    return Results.StatusCode((int)HttpStatusCode.InternalServerError);
});


app.Run();