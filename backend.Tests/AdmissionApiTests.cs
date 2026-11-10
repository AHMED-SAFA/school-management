using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using backend.Data;
using backend.Models;

namespace backend.Tests;

public class AdmissionApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _testDbName = "AdmissionTestDb_" + Guid.NewGuid().ToString("N");
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AdmissionApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptors = services.Where(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                      || d.ServiceType == typeof(DbContextOptions)
                      || d.ServiceType == typeof(AppDbContext)).ToList();

                foreach (var d in descriptors)
                {
                    services.Remove(d);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_testDbName);
                });
            });
        });
    }

    [Fact]
    public async Task CreateClass_ReturnsCreatedClass()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/classes", new
        {
            name = "Class 6 - Morning Shift",
            totalSeats = 120
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateClassResponse>(_jsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Class 6 - Morning Shift", body!.Name);
        Assert.Equal(120, body.TotalSeats);
        Assert.False(string.IsNullOrEmpty(body.Id));
    }

    [Fact]
    public async Task HoldAndConfirmFlow_WorksCorrectly()
    {
        var client = _factory.CreateClient();
        var studentId = "20152691234567890";

        // Create a class
        var createResponse = await client.PostAsJsonAsync("/classes", new
        {
            name = "Class 7 - Day Shift",
            totalSeats = 50
        });
        var createdClass = await createResponse.Content.ReadFromJsonAsync<CreateClassResponse>(_jsonOptions);
        Assert.NotNull(createdClass);

        // Create a hold
        var holdRequest = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass!.Id}/holds");
        holdRequest.Headers.Add("X-Student-Id", studentId);
        var holdResponse = await client.SendAsync(holdRequest);
        Assert.Equal(HttpStatusCode.Created, holdResponse.StatusCode);

        var hold = await holdResponse.Content.ReadFromJsonAsync<HoldResponse>(_jsonOptions);
        Assert.NotNull(hold);
        Assert.Equal("HELD", hold!.Status);
        Assert.Equal(studentId, hold.StudentId);

        // Confirm the hold
        var confirmRequest = new HttpRequestMessage(HttpMethod.Post, $"/holds/{hold.HoldId}/confirm");
        confirmRequest.Headers.Add("X-Student-Id", studentId);
        var confirmResponse = await client.SendAsync(confirmRequest);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var confirmed = await confirmResponse.Content.ReadFromJsonAsync<ConfirmResponse>(_jsonOptions);
        Assert.NotNull(confirmed);
        Assert.Equal("CONFIRMED", confirmed!.Status);

        // Verify class seat counts
        var classResponse = await client.GetFromJsonAsync<ClassResponse>($"/classes/{createdClass.Id}", _jsonOptions);
        Assert.NotNull(classResponse);
        Assert.Equal(1, classResponse!.ConfirmedSeats);
        Assert.Equal(0, classResponse.HeldSeats);
        Assert.Equal(49, classResponse.AvailableSeats);
    }

    [Fact]
    public async Task DuplicateHold_SameStudentSameClass_Returns409()
    {
        var client = _factory.CreateClient();
        var studentId = "20152691234567891";

        // Create a class
        var createResponse = await client.PostAsJsonAsync("/classes", new
        {
            name = "Class 8 - Morning Shift",
            totalSeats = 10
        });
        var createdClass = await createResponse.Content.ReadFromJsonAsync<CreateClassResponse>(_jsonOptions);
        Assert.NotNull(createdClass);

        // First hold should succeed
        var holdRequest1 = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass!.Id}/holds");
        holdRequest1.Headers.Add("X-Student-Id", studentId);
        var holdResponse1 = await client.SendAsync(holdRequest1);
        Assert.Equal(HttpStatusCode.Created, holdResponse1.StatusCode);

        // Second hold for same student same class should fail with 409
        var holdRequest2 = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass.Id}/holds");
        holdRequest2.Headers.Add("X-Student-Id", studentId);
        var holdResponse2 = await client.SendAsync(holdRequest2);
        Assert.Equal(HttpStatusCode.Conflict, holdResponse2.StatusCode);
    }

    [Fact]
    public async Task CancelHold_ReturnsNoContent()
    {
        var client = _factory.CreateClient();
        var studentId = "20152691234567892";

        // Create class
        var createResponse = await client.PostAsJsonAsync("/classes", new
        {
            name = "Class 9 - Day Shift",
            totalSeats = 30
        });
        var createdClass = await createResponse.Content.ReadFromJsonAsync<CreateClassResponse>(_jsonOptions);
        Assert.NotNull(createdClass);

        // Create hold
        var holdRequest = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass!.Id}/holds");
        holdRequest.Headers.Add("X-Student-Id", studentId);
        var holdResponse = await client.SendAsync(holdRequest);
        Assert.Equal(HttpStatusCode.Created, holdResponse.StatusCode);
        var hold = await holdResponse.Content.ReadFromJsonAsync<HoldResponse>(_jsonOptions);
        Assert.NotNull(hold);

        // Cancel the hold
        var cancelRequest = new HttpRequestMessage(HttpMethod.Delete, $"/holds/{hold!.HoldId}");
        cancelRequest.Headers.Add("X-Student-Id", studentId);
        var cancelResponse = await client.SendAsync(cancelRequest);
        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        // Verify status is CANCELLED
        var getHoldResponse = await client.GetFromJsonAsync<HoldResponse>($"/holds/{hold.HoldId}", _jsonOptions);
        Assert.NotNull(getHoldResponse);
        Assert.Equal("CANCELLED", getHoldResponse!.Status);
    }

    [Fact]
    public async Task GetMyHolds_ReturnsStudentHolds()
    {
        var client = _factory.CreateClient();
        var studentId = "20152691234567893";

        // Create class
        var createResponse = await client.PostAsJsonAsync("/classes", new
        {
            name = "Class 10 - Morning Shift",
            totalSeats = 20
        });
        var createdClass = await createResponse.Content.ReadFromJsonAsync<CreateClassResponse>(_jsonOptions);
        Assert.NotNull(createdClass);

        // Create hold
        var holdRequest = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass!.Id}/holds");
        holdRequest.Headers.Add("X-Student-Id", studentId);
        var holdResponse = await client.SendAsync(holdRequest);
        Assert.Equal(HttpStatusCode.Created, holdResponse.StatusCode);

        // Get my holds
        var myHoldsRequest = new HttpRequestMessage(HttpMethod.Get, "/me/holds");
        myHoldsRequest.Headers.Add("X-Student-Id", studentId);
        var myHoldsResponse = await client.SendAsync(myHoldsRequest);
        Assert.Equal(HttpStatusCode.OK, myHoldsResponse.StatusCode);

        var holds = await myHoldsResponse.Content.ReadFromJsonAsync<List<MyHoldResponse>>(_jsonOptions);
        Assert.NotNull(holds);
        Assert.Single(holds!);
        Assert.Equal("Class 10 - Morning Shift", holds[0].ClassName);
    }

    /// <summary>
    /// Concurrency test: send many hold requests simultaneously for a class with limited seats.
    /// Verifies that held + confirmed never exceeds totalSeats.
    /// </summary>
    [Fact]
    public async Task ConcurrencyTest_HoldsDoNotExceedTotalSeats()
    {
        var client = _factory.CreateClient();
        var totalSeats = 5;

        // Create a class with limited seats
        var createResponse = await client.PostAsJsonAsync("/classes", new
        {
            name = "Concurrency Test Class",
            totalSeats = totalSeats
        });
        var createdClass = await createResponse.Content.ReadFromJsonAsync<CreateClassResponse>(_jsonOptions);
        Assert.NotNull(createdClass);

        // Send many concurrent hold requests (more than available seats)
        var concurrentRequests = 20;
        var tasks = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < concurrentRequests; i++)
        {
            var studentId = $"2015269123456{i:D4}";
            var request = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass!.Id}/holds");
            request.Headers.Add("X-Student-Id", studentId);
            tasks.Add(client.SendAsync(request));
        }

        var responses = await Task.WhenAll(tasks);

        // Count successful holds
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        // Assert: successful holds should not exceed total seats
        Assert.True(successCount <= totalSeats,
            $"Expected at most {totalSeats} successful holds, but got {successCount}");

        // All responses should be either 201 (Created) or 409 (Conflict)
        foreach (var response in responses)
        {
            Assert.True(
                response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.Conflict,
                $"Unexpected status code: {response.StatusCode}");
        }

        // Verify class seat counts from API
        var classResponse = await client.GetFromJsonAsync<ClassResponse>($"/classes/{createdClass!.Id}", _jsonOptions);
        Assert.NotNull(classResponse);
        Assert.True(classResponse!.HeldSeats + classResponse.ConfirmedSeats <= totalSeats,
            $"held({classResponse.HeldSeats}) + confirmed({classResponse.ConfirmedSeats}) exceeds totalSeats({totalSeats})");
        Assert.Equal(totalSeats - (classResponse.HeldSeats + classResponse.ConfirmedSeats), classResponse.AvailableSeats);
    }
}
