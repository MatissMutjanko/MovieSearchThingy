using MovieSearchThingy.Models;

namespace MovieSearchThingy.Pages.Models
{
    public class MovieSearchResponse
    {
        public List<MovieSearchResult> Search { get; set; }
        public string TotalResults { get; set; }
        public string Response { get; set; }
        public string Error { get; set; }
    }
}
