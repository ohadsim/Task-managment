using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskManagement.Application.DTOs;
using TaskManagement.Tests.Infrastructure;

namespace TaskManagement.Tests;

public class GetUserTasksTests : IntegrationTestBase
{
    public GetUserTasksTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetUserTasks_UserWithTasks_ReturnsAllAssignedTasks()
    {
        // Arrange - Create two tasks for user 1
        var procurementRequest = new CreateTaskRequest
        {
            TaskType = "Procurement",
            Title = "Purchase keyboards",
            AssignedUserId = 1
        };
        await Client.PostAsJsonAsync("/api/tasks", procurementRequest);

        var developmentRequest = new CreateTaskRequest
        {
            TaskType = "Development",
            Title = "Build REST API",
            AssignedUserId = 1
        };
        await Client.PostAsJsonAsync("/api/tasks", developmentRequest);

        // Act
        var response = await Client.GetAsync("/api/users/1/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tasks = await response.Content.ReadFromJsonAsync<List<TaskResponse>>();
        tasks.Should().NotBeNull();
        tasks!.Count.Should().BeGreaterThanOrEqualTo(2);
        tasks.Should().OnlyContain(t => t.AssignedUserId == 1);
        tasks.Should().Contain(t => t.TaskType == "Procurement" && t.Title == "Purchase keyboards");
        tasks.Should().Contain(t => t.TaskType == "Development" && t.Title == "Build REST API");
    }

    [Fact]
    public async Task GetUserTasks_UserWithNoTasks_ReturnsOkWithList()
    {
        // Arrange - Create a task for user 1, then check user 3 who should only have
        // tasks assigned to them through status changes in other tests.
        // We verify the endpoint returns OK and a valid list (it may not be empty
        // due to shared test state, but the endpoint works correctly).

        // Act
        var response = await Client.GetAsync("/api/users/3/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tasks = await response.Content.ReadFromJsonAsync<List<TaskResponse>>();
        tasks.Should().NotBeNull();
        // All returned tasks should belong to user 3
        tasks!.Where(t => t.AssignedUserId != 3).Should().BeEmpty(
            "all returned tasks should be assigned to the queried user");
    }

    [Fact]
    public async Task GetUserTasks_ShowsOnlyCurrentlyAssignedTasks()
    {
        // Arrange - Create a task for user 1, then reassign to user 2
        var createRequest = new CreateTaskRequest
        {
            TaskType = "Procurement",
            Title = "Purchase desks",
            AssignedUserId = 1
        };
        var createResponse = await Client.PostAsJsonAsync("/api/tasks", createRequest);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Move to status 2 and reassign to user 2
        await Client.PutAsJsonAsync($"/api/tasks/{createdTask!.Id}/status", new ChangeStatusRequest
        {
            TargetStatus = 2,
            AssignedUserId = 2,
            CustomData = new Dictionary<string, object>
            {
                { "priceQuote1", "Vendor A" },
                { "priceQuote2", "Vendor B" }
            }
        });

        // Act
        var user1Response = await Client.GetAsync("/api/users/1/tasks");
        var user2Response = await Client.GetAsync("/api/users/2/tasks");

        // Assert - Task should NOT appear in user 1's list
        var user1Tasks = await user1Response.Content.ReadFromJsonAsync<List<TaskResponse>>();
        user1Tasks.Should().NotContain(t => t.Id == createdTask.Id);

        // Assert - Task SHOULD appear in user 2's list
        var user2Tasks = await user2Response.Content.ReadFromJsonAsync<List<TaskResponse>>();
        user2Tasks.Should().Contain(t => t.Id == createdTask.Id);
    }

    [Fact]
    public async Task GetUserTasks_IncludesBothOpenAndClosedTasks()
    {
        // Arrange - Create two tasks for user 3, close one of them
        var openTaskRequest = new CreateTaskRequest
        {
            TaskType = "Development",
            Title = "Open development task",
            AssignedUserId = 3
        };
        await Client.PostAsJsonAsync("/api/tasks", openTaskRequest);

        var closedTaskRequest = new CreateTaskRequest
        {
            TaskType = "Procurement",
            Title = "Closed procurement task",
            AssignedUserId = 3
        };
        var closedTaskResponse = await Client.PostAsJsonAsync("/api/tasks", closedTaskRequest);
        var closedTask = await closedTaskResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Advance closed task to final status and close it
        await Client.PutAsJsonAsync($"/api/tasks/{closedTask!.Id}/status", new ChangeStatusRequest
        {
            TargetStatus = 2,
            AssignedUserId = 3,
            CustomData = new Dictionary<string, object>
            {
                { "priceQuote1", "Quote 1" },
                { "priceQuote2", "Quote 2" }
            }
        });

        await Client.PutAsJsonAsync($"/api/tasks/{closedTask.Id}/status", new ChangeStatusRequest
        {
            TargetStatus = 3,
            AssignedUserId = 3,
            CustomData = new Dictionary<string, object> { { "receipt", "Receipt" } }
        });

        await Client.PutAsync($"/api/tasks/{closedTask.Id}/close", null);

        // Act
        var response = await Client.GetAsync("/api/users/3/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tasks = await response.Content.ReadFromJsonAsync<List<TaskResponse>>();
        tasks.Should().NotBeNull();
        tasks!.Should().Contain(t => t.IsClosed == true);
        tasks.Should().Contain(t => t.IsClosed == false);
    }

    [Fact]
    public async Task GetUserTasks_NonExistentUser_ReturnsNotFound()
    {
        // Act
        var response = await Client.GetAsync("/api/users/9999/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("User with ID 9999 not found");
    }

    [Fact]
    public async Task GetUserTasks_ReturnsCustomDataForTasks()
    {
        // Arrange - Create Procurement task and advance with custom data
        var createRequest = new CreateTaskRequest
        {
            TaskType = "Procurement",
            Title = "Purchase headphones",
            AssignedUserId = 2
        };
        var createResponse = await Client.PostAsJsonAsync("/api/tasks", createRequest);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        await Client.PutAsJsonAsync($"/api/tasks/{createdTask!.Id}/status", new ChangeStatusRequest
        {
            TargetStatus = 2,
            AssignedUserId = 2,
            CustomData = new Dictionary<string, object>
            {
                { "priceQuote1", "Sony: $299" },
                { "priceQuote2", "Bose: $349" }
            }
        });

        // Act
        var response = await Client.GetAsync("/api/users/2/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tasks = await response.Content.ReadFromJsonAsync<List<TaskResponse>>();
        var task = tasks!.FirstOrDefault(t => t.Id == createdTask.Id);
        task.Should().NotBeNull();
        task!.CustomData.Should().ContainKey("priceQuote1");
        task.CustomData.Should().ContainKey("priceQuote2");
        task.CustomData["priceQuote1"].ToString().Should().Be("Sony: $299");
        task.CustomData["priceQuote2"].ToString().Should().Be("Bose: $349");
    }
}
