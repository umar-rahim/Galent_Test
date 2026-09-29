using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Api;
using Api.Data;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Api.Tests;

public sealed class SmokeTests : IClassFixture<SmokeTests.ApiFactory>
{
    private readonly ApiFactory _factory;

    public SmokeTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task AuthenticationSubmissionAuthorizationAndValidationWorkTogether()
    {
        using var client = _factory.CreateClient();
        var admin = await Login(client, "admin@example.test", "Admin-Test-123!");
        var preparer = await Login(client, "preparer@example.test", "Preparer-Test-123!");
        var reviewer = await Login(client, "reviewer@example.test", "Reviewer-Test-123!");

        using var anonymous = await client.GetAsync("/api/submissions");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        using var preparerClient = Authenticated(preparer);
        using var reviewerClient = Authenticated(reviewer);
        using var adminClient = Authenticated(admin);

        using var reviewerCreate = await reviewerClient.PostAsJsonAsync("/api/submissions", new { });
        Assert.Equal(HttpStatusCode.Forbidden, reviewerCreate.StatusCode);

        using var created = await preparerClient.PostAsJsonAsync("/api/submissions", new { });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdBody = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var submissionId = createdBody.RootElement.GetProperty("id").GetGuid();

        var save = new
        {
            form = new
            {
                taxpayerFirstName = "Smoke",
                taxpayerLastName = "Test",
                taxpayerSsn = "123-45-6789",
                filingStatus = "Single",
                line1a = 60000m,
                line2b = 100m,
                line12 = 15000m,
                line25a = 10000m,
                line26 = 1000m,
                routingNumber = "021000021",
                accountNumber = "1234567890",
                bankAccountType = "Checking"
            },
        };
        using var saved = await preparerClient.PutAsJsonAsync($"/api/submissions/{submissionId}", save);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

        using var get = await preparerClient.GetAsync($"/api/submissions/{submissionId}");
        get.EnsureSuccessStatusCode();
        using var data = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        Assert.Equal("123-45-6789", data.RootElement.GetProperty("form").GetProperty("taxpayerSsn").GetString());
        Assert.Equal(60100m, data.RootElement.GetProperty("form").GetProperty("line11a").GetDecimal());
        Assert.Equal("1234567890", data.RootElement.GetProperty("form").GetProperty("accountNumber").GetString());

        using var reviewerGet = await reviewerClient.GetAsync($"/api/submissions/{submissionId}");
        reviewerGet.EnsureSuccessStatusCode();
        using var reviewerData = JsonDocument.Parse(await reviewerGet.Content.ReadAsStringAsync());
        Assert.Equal("***-**-6789", reviewerData.RootElement.GetProperty("form").GetProperty("taxpayerSsn").GetString());
        Assert.Equal("******7890", reviewerData.RootElement.GetProperty("form").GetProperty("accountNumber").GetString());
        Assert.Null(reviewerData.RootElement.GetProperty("form").GetProperty("designeePin").GetString());

        using var reviewerSave = await reviewerClient.PutAsJsonAsync($"/api/submissions/{submissionId}", save);
        Assert.Equal(HttpStatusCode.Forbidden, reviewerSave.StatusCode);

        using var adminUsers = await adminClient.GetAsync("/api/admin/users");
        adminUsers.EnsureSuccessStatusCode();

        using var validate = await preparerClient.PostAsync($"/api/submissions/{submissionId}/validate", null);
        validate.EnsureSuccessStatusCode();

        using var submit = await preparerClient.PostAsync($"/api/submissions/{submissionId}/submit", null);
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
        using var validateSubmitted = await preparerClient.PostAsync($"/api/submissions/{submissionId}/validate", null);
        Assert.Equal(HttpStatusCode.Conflict, validateSubmitted.StatusCode);

        using var logout = await preparerClient.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using var revoked = await preparerClient.GetAsync("/api/submissions");
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
    }

    private static async Task<string> Login(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Login failed with {(int)response.StatusCode}: {content}");
        using var body = JsonDocument.Parse(content);
        return body.RootElement.GetProperty("accessToken").GetString()!;
    }

    private HttpClient Authenticated(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _connection.Open();
            var settings = new Dictionary<string, string?>
            {
                ["JWT_SECRET"] = "smoke-test-secret-value-at-least-32-characters-long",
                ["ADMIN_EMAIL"] = "admin@example.test",
                ["ADMIN_PASSWORD"] = "Admin-Test-123!",
                ["PREPARER_EMAIL"] = "preparer@example.test",
                ["PREPARER_PASSWORD"] = "Preparer-Test-123!",
                ["REVIEWER_EMAIL"] = "reviewer@example.test",
                ["REVIEWER_PASSWORD"] = "Reviewer-Test-123!"
            };
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                _connection.Dispose();
        }
    }
}
