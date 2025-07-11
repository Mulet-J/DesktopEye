
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DesktopEye.Common.Domain.Models.Dictionary;

namespace DesktopEye.Common.Infrastructure.Services.Dictionary;


public class WiktionaryService : IWiktionaryService
{
    private readonly HttpClient _client;
    private readonly Dictionary<string, WiktionaryResponse> _cache = new();


    public WiktionaryService(IHttpClientFactory clientFactory)
    {
        _client = clientFactory.CreateClient("DesktopEyeClient");
    }
    
    public async Task<WiktionaryResponse> GetDefinitionsAsync(string term)
    {
        var normalizedTerm = term.ToLowerInvariant();
    
        if (_cache.TryGetValue(normalizedTerm, out var cachedResponse))
            return cachedResponse;
        
        var response = await FetchDefinitionsFromApiAsync(normalizedTerm);
        _cache[normalizedTerm] = response;
        return response;
    }
    
    private async Task<WiktionaryResponse> FetchDefinitionsFromApiAsync(string term)
    {
        var url = $"https://en.wiktionary.org/api/rest_v1/page/definition/{Uri.EscapeDataString(term)}";
        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<WiktionaryResponse>(json);
    }
}