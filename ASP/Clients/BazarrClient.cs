using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;
using JanitorAspNet.Models;

namespace JanitorAspNet.Clients
{
    public interface IBazarrClient
    {
        [Get("/api/movies")]
        Task<List<BazarrMovie>> GetMoviesAsync();

        [Get("/api/series")]
        Task<List<BazarrSeries>> GetSeriesAsync();

        [Get("/api/movies/{movieId}/subtitles")]
        Task<List<BazarrSubtitle>> GetMovieSubtitlesAsync(int movieId);

        [Get("/api/series/{seriesId}/subtitles")]
        Task<List<BazarrSubtitle>> GetSeriesSubtitlesAsync(int seriesId);
    }
}
