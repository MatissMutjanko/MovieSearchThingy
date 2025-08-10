using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MovieSearchThingy.Pages;
using System.Collections.Generic;
using System.Text.Json;
using System.Net;
using System.Threading.Tasks;

public class MovieThingyTests
{
    private IndexModel CreateModelWithSession(out TestSession session)
    {
        var logger = Mock.Of<ILogger<IndexModel>>();
        var httpClientFactory = Mock.Of<IHttpClientFactory>();
        var model = new IndexModel(logger, httpClientFactory);

        var context = new DefaultHttpContext();
        session = new TestSession();
        context.Session = session;
        model.PageContext = new PageContext { HttpContext = context };
        return model;
    }

    [Fact]
    public void SaveSearchQuery_SavesAndLimitsToFive()
    {
        var model = CreateModelWithSession(out var session);

        for (int i = 1; i <= 7; i++)
            model.SaveSearchQuery($"query{i}");

        var searches = session.GetString("LastSearches");
        var list = JsonSerializer.Deserialize<List<string>>(searches);
        Assert.Equal(5, list.Count);
        Assert.Equal("query7", list[0]);
        Assert.Equal("query3", list[4]);
    }

    [Fact]
    public async Task OnPostSearchAsync_ReturnsResults()
    {
        var logger = Mock.Of<ILogger<IndexModel>>();
        var handler = new MockHttpMessageHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"Search\":[{\"Title\":\"Test Movie\",\"Year\":\"2020\",\"imdbID\":\"tt123\",\"Type\":\"movie\",\"Poster\":\"N/A\"}],\"Response\":\"True\"}")
            }
        };
        var client = new HttpClient(handler);
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var model = new IndexModel(logger, httpClientFactory.Object)
        {
            SearchQuery = "Test"
        };
        var context = new DefaultHttpContext();
        context.Session = new TestSession();
        model.PageContext = new PageContext { HttpContext = context };

        var result = await model.OnPostSearchAsync();

        Assert.NotNull(model.SearchResults);
        Assert.Single(model.SearchResults);
        Assert.Equal("Test Movie", model.SearchResults[0].Title);
    }

    [Fact]
    public async Task OnPostSearchAsync_ReturnsNotFound()
    {
        var logger = Mock.Of<ILogger<IndexModel>>();
        var handler = new MockHttpMessageHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"Response\":\"False\",\"Error\":\"Movie not found!\"}")
            }
        };
        var client = new HttpClient(handler);
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var model = new IndexModel(logger, httpClientFactory.Object)
        {
            SearchQuery = "NonExistentMovie"
        };
        var context = new DefaultHttpContext();
        context.Session = new TestSession();
        model.PageContext = new PageContext { HttpContext = context };

        var result = await model.OnPostSearchAsync();

        Assert.NotNull(model.SearchResults);
        Assert.Empty(model.SearchResults);
    }

    // Helper classes for mocking
    class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage Response { get; set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            => Task.FromResult(Response);
    }

    class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public IEnumerable<string> Keys => _store.Keys;
        public string Id { get; } = System.Guid.NewGuid().ToString();
        public bool IsAvailable => true;
        public void Clear() => _store.Clear();
        public Task CommitAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value);

        // Helpers for string session values
        public void SetString(string key, string value) => Set(key, System.Text.Encoding.UTF8.GetBytes(value));
        public string GetString(string key)
        {
            return TryGetValue(key, out var data) ? System.Text.Encoding.UTF8.GetString(data) : null;
        }
    }
}