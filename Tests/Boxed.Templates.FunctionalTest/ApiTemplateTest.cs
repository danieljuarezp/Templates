namespace Boxed.Templates.FunctionalTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using ApiTemplate.ViewModels;
    using Boxed.AspNetCore;
    using Boxed.DotnetNewTest;
    using Xunit;
    using Xunit.Abstractions;

    public class ApiTemplateTest
    {
        public ApiTemplateTest(ITestOutputHelper testOutputHelper)
        {
            TestLogger.WriteMessage = testOutputHelper.WriteLine;
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            DotnetNew.InstallAsync<ApiTemplateTest>("ApiTemplate.sln").Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
        }

        [Theory]
        [Trait("IsUsingDotnetRun", "false")]
        [InlineData("Default")]
        [InlineData("NoForwardedHeaders", "forwarded-headers=false")]
        [InlineData("NoHostFiltering", "host-filtering=false")]
        [InlineData("NoFwdHeadersOrHostFiltering", "forwarded-headers=false", "host-filtering=false")]
        public async Task RestoreAndBuild_Default_SuccessfulAsync(string name, params string[] arguments)
        {
            using (var tempDirectory = TempDirectory.NewTempDirectory())
            {
                var dictionary = arguments
                    .Select(x => x.Split('=', StringSplitOptions.RemoveEmptyEntries))
                    .ToDictionary(x => x.First(), x => x.Last());
                var project = await tempDirectory.DotnetNewAsync("api", name, dictionary);
                await project.DotnetRestoreAsync();
                await project.DotnetBuildAsync();
            }
        }

        [Fact]
        [Trait("IsUsingDotnetRun", "true")]
        public async Task Run_Default_SuccessfulAsync()
        {
            using (var tempDirectory = TempDirectory.NewTempDirectory())
            {
                var project = await tempDirectory.DotnetNewAsync("api", "Default");
                await project.DotnetRestoreAsync();
                await project.DotnetBuildAsync();
                await project.DotnetRunAsync(
                    @"Source\Default",
                    ReadinessCheck.StatusSelf,
                    async (httpClient, httpsClient) =>
                    {
                        var httpResponse = await httpClient.GetAsync("status");
                        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);

                        var statusResponse = await httpsClient.GetAsync("status");
                        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
                        Assert.Equal(ContentType.Text, statusResponse.Content.Headers.ContentType.MediaType);

                        var statusSelfResponse = await httpsClient.GetAsync("status/self");
                        Assert.Equal(HttpStatusCode.OK, statusSelfResponse.StatusCode);
                        Assert.Equal(ContentType.Text, statusSelfResponse.Content.Headers.ContentType.MediaType);

                        var carsResponse = await httpsClient.GetAsync("cars");
                        Assert.Equal(HttpStatusCode.OK, carsResponse.StatusCode);
                        Assert.Equal(ContentType.RestfulJson, carsResponse.Content.Headers.ContentType.MediaType);

                        var postCarResponse = await httpsClient.PostAsJsonAsync("cars", new SaveCar());
                        Assert.Equal(HttpStatusCode.BadRequest, postCarResponse.StatusCode);
                        Assert.Equal(ContentType.ProblemJson, postCarResponse.Content.Headers.ContentType.MediaType);

                        var notAcceptableCarsRequest = new HttpRequestMessage(HttpMethod.Get, "cars");
                        notAcceptableCarsRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(ContentType.Text));
                        var notAcceptableCarsResponse = await httpsClient.SendAsync(notAcceptableCarsRequest);
                        Assert.Equal(HttpStatusCode.NotAcceptable, notAcceptableCarsResponse.StatusCode);

                        var swaggerJsonResponse = await httpsClient.GetAsync("swagger/v1/swagger.json");
                        Assert.Equal(HttpStatusCode.OK, swaggerJsonResponse.StatusCode);
                        Assert.Equal(ContentType.Json, swaggerJsonResponse.Content.Headers.ContentType.MediaType);

                        var robotsTxtResponse = await httpsClient.GetAsync("robots.txt");
                        Assert.Equal(HttpStatusCode.OK, robotsTxtResponse.StatusCode);
                        Assert.Equal(ContentType.Text, robotsTxtResponse.Content.Headers.ContentType.MediaType);

                        var securityTxtResponse = await httpsClient.GetAsync(".well-known/security.txt");
                        Assert.Equal(HttpStatusCode.OK, securityTxtResponse.StatusCode);
                        Assert.Equal(ContentType.Text, securityTxtResponse.Content.Headers.ContentType.MediaType);

                        var humansTxtResponse = await httpsClient.GetAsync("humans.txt");
                        Assert.Equal(HttpStatusCode.OK, humansTxtResponse.StatusCode);
                        Assert.Equal(ContentType.Text, humansTxtResponse.Content.Headers.ContentType.MediaType);
                    },
                    timeout: TimeSpan.FromMinutes(2));
            }
        }

        [Fact]
        [Trait("IsUsingDotnetRun", "true")]
        public async Task Run_HealthCheckFalse_SuccessfulAsync()
        {
            using (var tempDirectory = TempDirectory.NewTempDirectory())
            {
                var project = await tempDirectory.DotnetNewAsync(
                    "api",
                    "HealthCheckFalse",
                    new Dictionary<string, string>()
                    {
                        { "health-check", "false" },
                    });
                await project.DotnetRestoreAsync();
                await project.DotnetBuildAsync();
                await project.DotnetRunAsync(
                    @"Source\HealthCheckFalse",
                    ReadinessCheck.Favicon,
                    async (httpClient, httpsClient) =>
                    {
                        var statusResponse = await httpsClient.GetAsync("status");
                        Assert.Equal(HttpStatusCode.NotFound, statusResponse.StatusCode);

                        var statusSelfResponse = await httpsClient.GetAsync("status/self");
                        Assert.Equal(HttpStatusCode.NotFound, statusSelfResponse.StatusCode);
                    });
            }
        }

        [Fact]
        [Trait("IsUsingDotnetRun", "true")]
        public async Task Run_HttpsEverywhereFalse_SuccessfulAsync()
        {
            using (var tempDirectory = TempDirectory.NewTempDirectory())
            {
                var project = await tempDirectory.DotnetNewAsync(
                    "api",
                    "HttpsEverywhereFalse",
                    new Dictionary<string, string>()
                    {
                        { "https-everywhere", "false" },
                    });
                await project.DotnetRestoreAsync();
                await project.DotnetBuildAsync();
                await project.DotnetRunAsync(
                    @"Source\HttpsEverywhereFalse",
                    ReadinessCheck.StatusSelfOverHttp,
                    async (httpClient, httpsClient) =>
                    {
                        var statusResponse = await httpClient.GetAsync("status");
                        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
                    });
            }
        }

        [Fact]
        [Trait("IsUsingDotnetRun", "true")]
        public async Task Run_SwaggerFalse_SuccessfulAsync()
        {
            using (var tempDirectory = TempDirectory.NewTempDirectory())
            {
                var project = await tempDirectory.DotnetNewAsync(
                    "api",
                    "SwaggerFalse",
                    new Dictionary<string, string>()
                    {
                        { "swagger", "false" },
                    });
                await project.DotnetRestoreAsync();
                await project.DotnetBuildAsync();
                await project.DotnetRunAsync(
                    @"Source\SwaggerFalse",
                    ReadinessCheck.StatusSelf,
                    async (httpClient, httpsClient) =>
                    {
                        var swaggerJsonResponse = await httpsClient.GetAsync("swagger/v1/swagger.json");
                        Assert.Equal(HttpStatusCode.NotFound, swaggerJsonResponse.StatusCode);
                    });
            }
        }
    }
}
