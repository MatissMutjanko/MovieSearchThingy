using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MovieSearchThingy.Models;
using System.Net.Http;
using System.Text.Json;

namespace MovieSearchThingy.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private const string ApiKey = "f518d0fe";
        private const string OmdbApiUrl = "http://www.omdbapi.com/";

        [BindProperty]
        public string SearchQuery { get; set; }
        public List<string> LastSearches { get; set; } = new();
        public List<MovieSearchResult> SearchResults { get; set; }
        public MovieDetail SelectedMovie { get; set; }
        [BindProperty]
        public string SelectedImdbId { get; set; }

        public IndexModel(ILogger<IndexModel> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public void OnGet()
        {
            LoadLastSearches();
        }

        public async Task<IActionResult> OnPostSearchAsync()
        {
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                SaveSearchQuery(SearchQuery);
                var client = _httpClientFactory.CreateClient();
                var url = $"{OmdbApiUrl}?apikey={ApiKey}&s={Uri.EscapeDataString(SearchQuery)}";
                var response = await client.GetStringAsync(url);
                var result = JsonSerializer.Deserialize<MovieSearchResponse>(response);
                SearchResults = result?.Search ?? new List<MovieSearchResult>();
            }
            LoadLastSearches();
            return Page();
        }

        public async Task<IActionResult> OnPostSelectAsync()
        {
            if (!string.IsNullOrWhiteSpace(SelectedImdbId))
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{OmdbApiUrl}?apikey={ApiKey}&i={Uri.EscapeDataString(SelectedImdbId)}&plot=full";
                var response = await client.GetStringAsync(url);
                SelectedMovie = JsonSerializer.Deserialize<MovieDetail>(response);
            }
            LoadLastSearches();
            return Page();
        }

        public void SaveSearchQuery(string query)
        {
            var searches = HttpContext.Session.GetString("LastSearches");
            var list = string.IsNullOrEmpty(searches) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(searches);
            list.Remove(query);
            list.Insert(0, query);
            if (list.Count > 5) list = list.Take(5).ToList();
            HttpContext.Session.SetString("LastSearches", JsonSerializer.Serialize(list));
        }

        private void LoadLastSearches()
        {
            var searches = HttpContext.Session.GetString("LastSearches");
            LastSearches = string.IsNullOrEmpty(searches) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(searches);
        }
    }
}
