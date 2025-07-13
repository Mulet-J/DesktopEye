using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using DesktopEye.Common.Domain.Models.Dictionary;
using DesktopEye.Common.Infrastructure.Services.Dictionary;
using Microsoft.Extensions.Logging;

namespace DesktopEye.Common.Domain.Features.Dictionary;

/// <summary>
/// Service that fetches word definitions from Wiktionary API with caching capability.
/// </summary>
public class WiktionaryService : IWiktionaryService
{
    private readonly HttpClient _client;
    private readonly Dictionary<string, WiktionaryResponse> _cache = new();
    private readonly ILogger<WiktionaryService> _logger;
    private readonly Bugsnag.IClient _bugsnag;

    /// <summary>
    /// Initializes a new instance of the WiktionaryService.
    /// </summary>
    /// <param name="clientFactory">The HTTP client factory to create the client</param>
    /// <param name="logger">The logger for recording operations and errors</param>
    /// <param name="bugsnag">The Bugsnag client for error reporting</param>
    public WiktionaryService(IHttpClientFactory clientFactory, ILogger<WiktionaryService> logger, Bugsnag.IClient bugsnag)
    {
        _client = clientFactory.CreateClient("DesktopEyeClient");
        _logger = logger;
        _bugsnag = bugsnag;
    }

    /// <summary>
    /// Gets definitions for the specified term, using cache if available.
    /// </summary>
    /// <param name="term">The term to look up</param>
    /// <returns>A response containing definitions grouped by language</returns>
    public async Task<WiktionaryResponse> GetDefinitionsAsync(string term)
    {
        try
        {
            var normalizedTerm = term.ToLowerInvariant();
            
            _logger.LogInformation("Recherche de définitions pour le terme : {Term}", normalizedTerm);

            if (_cache.TryGetValue(normalizedTerm, out var cachedResponse))
            {
                _logger.LogDebug("Définitions trouvées dans le cache pour : {Term}", normalizedTerm);
                return cachedResponse;
            }

            _logger.LogDebug("Terme non trouvé dans le cache, appel à l'API Wiktionary : {Term}", normalizedTerm);
            var response = await FetchDefinitionsFromApiAsync(normalizedTerm);
            
            _logger.LogDebug("Définitions récupérées depuis l'API pour : {Term}", normalizedTerm);
            _cache[normalizedTerm] = response;
            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erreur lors de la requête HTTP pour obtenir les définitions du terme : {Term}. StatusCode: {StatusCode}", 
                term, ex.StatusCode);
            _bugsnag.Notify(ex);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erreur lors de la désérialisation de la réponse pour le terme : {Term}", term);
            _bugsnag.Notify(ex);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur inattendue lors de la récupération des définitions pour le terme : {Term}", term);
            _bugsnag.Notify(ex);
            throw;
        }
    }

    /// <summary>
    /// Fetches definitions from the Wiktionary API.
    /// </summary>
    /// <param name="term">The normalized term to look up</param>
    /// <returns>A response containing definitions grouped by language</returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails</exception>
    private async Task<WiktionaryResponse> FetchDefinitionsFromApiAsync(string term)
    {
        var url = $"https://en.wiktionary.org/api/rest_v1/page/definition/{Uri.EscapeDataString(term)}";
        _logger.LogDebug("Envoi de la requête à l'URL : {Url}", url);
        
        var response = await _client.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("La requête à l'API Wiktionary a échoué pour le terme : {Term}. StatusCode: {StatusCode}", 
                term, response.StatusCode);
        }
        
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        
        _logger.LogTrace("Réponse JSON reçue : {JsonLength} caractères", json.Length);
        
        return JsonSerializer.Deserialize<WiktionaryResponse>(json);
    }
}