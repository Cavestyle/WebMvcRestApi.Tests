using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using WebMvcRestApi.Models;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace WebMvcRestApi.Tests
{    
    public class IntegrationTests : IClassFixture<WebApplicationFactory<WebMvcRestApi.Startup>>
    {
        private readonly HttpClient _httpClient;

        public IntegrationTests(WebApplicationFactory<Startup> factory)
        {
            //Arrange
            _httpClient = factory.CreateClient();
        }

        [Fact]
        public async Task GetEndPoint_ShouldReturnAlbumModel_WhenGivenArtistName()
        {
            // Arrange, begins at the constructor        

            // Act, call my endpoint
            HttpResponseMessage response = await _httpClient.GetAsync("api/albums?artistName=Bloc Party");

            // Assert                   
            string results = await response.Content.ReadAsStringAsync();
            AlbumModel actual = JsonConvert.DeserializeObject<AlbumModel>(results);
           
            actual.ResultCount.Should().Be(37);                   
        }

        [Fact]
        public async Task GetEndPoint_ShouldReturnNotFound_WhenMissingArtistName()
        {
            // Arrange, begins at the constructor        

            // Act, call my endpoint
            HttpResponseMessage response = await _httpClient.GetAsync("api/albums?artistName=");

            // Assert                   
            string results = await response.Content.ReadAsStringAsync();
            AlbumModel actual = JsonConvert.DeserializeObject<AlbumModel>(results);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);            
        }
    }
}
