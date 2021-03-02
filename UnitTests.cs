using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using WebMvcRestApi.Controllers;
using WebMvcRestApi.Interfaces;
using WebMvcRestApi.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace WebMvcRestApi.Tests
{
    public class UnitTests
    {
        [Fact]
        public async Task Get_ShouldReturnAlbumModel_WhenGivenValidUrl()
        {
            // Arrange
            const string url = "https://itunes.apple.com/search?term={artistName}&media=music&entity=album&attribute=artistTerm&limit=200";
            AlbumModel expected = GetTestAlbumModel();

            Mock<IAppleItunesClient> mockClient = new Mock<IAppleItunesClient>();
            mockClient.Setup(x => x.LoadAlbums(It.IsAny<string>())).ReturnsAsync(expected);

            Mock<IConfiguration> mockConfig = new Mock<IConfiguration>();
            mockConfig.SetupGet(x => x[It.Is<string>(s => s == "Urls:SearchAlbumsByArtist")]).Returns(url);

            AlbumsController controller = new AlbumsController(mockClient.Object, mockConfig.Object);

            // Act
            ActionResult<AlbumModel> actual = await controller.Get("Bloc Party");

            // Assert                     
            actual.Value.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task Get_ShouldReturnNotFound_WhenAlbumModelIsNull()
        {
            // Arrange
            const string url = "https://itunes.apple.com/search?term={artistName}&media=music&entity=album&attribute=artistTerm&limit=200";

            Mock<IAppleItunesClient> mockClient = new Mock<IAppleItunesClient>();
            mockClient.Setup(x => x.LoadAlbums(It.IsAny<string>()));

            Mock<IConfiguration> mockConfig = new Mock<IConfiguration>();
            mockConfig.SetupGet(x => x[It.Is<string>(s => s == "Urls:SearchAlbumsByArtist")]).Returns(url);

            AlbumsController controller = new AlbumsController(mockClient.Object, mockConfig.Object);

            // Act
            ActionResult<AlbumModel> albums = await controller.Get("Bloc Party");

            // Assert    
            var actual = albums.Result as NotFoundResult;             
            actual.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task LoadAlbums_ShouldReturnAlbumModel_WhenGivenValidUrl()
        {
            // Arrange
            const string url = "https://itunes.apple.com/search?term=Bloc+Party&media=music&entity=album&attribute=artistTerm&limit=200";
            AlbumModel expected = GetTestAlbumModel();

            Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            mockHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(expected), Encoding.UTF8, "application/json")
                })
                .Verifiable();

            HttpClient httpClient = new HttpClient(mockHandler.Object);
            AppleItunesClient appleItunesClient = new AppleItunesClient(httpClient);

            // Act
            AlbumModel actual = await appleItunesClient.LoadAlbums(url);

            // Assert
            mockHandler.Protected().Verify("SendAsync", Times.Exactly(1), ItExpr.Is<HttpRequestMessage>
                (req => req.Method == HttpMethod.Get && req.RequestUri == new Uri(url)), ItExpr.IsAny<CancellationToken>());

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task LoadAlbums_ShouldReturnNull_WhenNotFound()
        {
            // Arrange      
            Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            mockHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.NotFound,
                })
                .Verifiable();

            HttpClient httpClient = new HttpClient(mockHandler.Object);
            AppleItunesClient appleItunesClient = new AppleItunesClient(httpClient);

            // Act
            AlbumModel actual = await appleItunesClient.LoadAlbums("https://bad.url.com/");

            // Assert         
            actual.Should().BeNull();
        }

        public static AlbumModel GetTestAlbumModel()
        {
            List<Result> testResults = new List<Result>
            {
                new Result { ArtistName = "Bloc Party", CollectionName = "Silent Alarm", ReleaseDate = new DateTime(2005, 02, 02), PrimaryGenreName = "Indie"},
                new Result { ArtistName = "Bloc Party", CollectionName = "Hymns", ReleaseDate = new DateTime(2016, 01, 29), PrimaryGenreName = "Indie"},
            };
            return new AlbumModel { ResultCount = 2, Results = testResults };
        }
    }
}