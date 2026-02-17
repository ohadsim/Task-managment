using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskManagement.Application.DTOs;
using TaskManagement.Tests.Infrastructure;

namespace TaskManagement.Tests;

public class CloseTaskTests : IntegrationTestBase
{
    public CloseTaskTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CloseTask_ProcurementFromFinalStatus_Succeeds()
    {
        // Arrange - Create Procurement task and advance to final status (3)
        var createRequest = new CreateTaskRequest
        {
            TaskType = "Procurement",
            Title = "Purchase cloud credits",
            AssignedUserId = 1
        };
        var createResponse = await Client.PostAsJsonAsync("/api/tasks", createRequest);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Move 1 -> 2
        await Client.PutAsJsonAsync($"/api/tasks/{createdTask!.Id}/status", new ChangeStatusRequest
        {
            TargetStatus = 2,
            AssignedUserId = 2,
            CustomData = new Dictionary<string, object>
            {
                { "priceQuote1", "AWS: $1200/mo" },
                { "priceQuote2", "Azure: $1150/mo" }
            }
        });

        // Move 2 -> 3
        await Client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}/status", new ChangeStatusRequest
        {
            TargetStatus = 3,
            AssignedUserId = 3,
            CustomData = new Dictionary<string, object>
            {
                { "receipt", "Order #67890" }
            }
        });

        // Act - Close the task
        var response = await Client.PutAsync($"/api/tasks/{createdTask.Id}/close", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var closedTask = await response.Content.ReadFromJsonAsync<TaskResponse>();
        closedTask.Should().NotBeNull();
        closedTask!.IsClosed.Should().BeTrue();
        closedTask.CurrentStatus.Should().Be(3);
    }

    [Fact]
    public async Task CloseTask_DevelopmentFromFinalStatus_Succeeds()
    {
        // Arrange - Create Development task and advance to final status (4)
        var createRequest = new CreateTaskRequest
        {
            TaskType = "Development",
            Title = "Build analytics dashboard",
            AssignedUserId = 1
        };
        var createResponse = await Client.PostAsJsonAsync("/api/tasks", createRequest);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Move through all statuses
        await Client.PutAsJsonAsync($"/api/tasks/{createdTask!.Id}/status", new ChangeStatusRequest
        {
            TargetStatus = 2,
            AssignedUserId = 2,
            CustomData = new Dictionary<string, object> { { "specificationText", "Dashboard spec" } }
        });

        await Client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}/status", new ChangeStatusRequest
        {
            TargetStatus = 3,
            AssignedUserId = 3,
            CustomData = new Dictionary<string, object> { { "branchName", "feature/dashboard" } }
        });

        await Client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}/status", new ChangeStatusRequest
        {
            TargetStatus = 4,
            AssignedUserId = 4,
            CustomData = new Dictionary<string, object> { { "versionNumber", "v2.0.0" } }
        });

        // Act - Close the task
        var response = await Client.PutAsync($"/api/tasks/{createdTask.Id}/close", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var closedTask = await response.Content.ReadFromJsonAsync<TaskResponse>();
        closedTask.Should().NotBeNull();
        closedTask!.IsClosed.Should().BeTrue();
        closedTask.CurrentStatus.Should().Be(4);
    }

    [Fact]
    public async Task CloseTask_FromNonFinalStatus_ReturnsBadRequest()
    {
        // Arrange - Create Procurement task at status 1
        var createRequest = new CreateTaskRequest
        {
            TaskType = "Procurement",
            Title = "Purchase monitors",
            AssignedUserId = 1
        };
        var createResponse = await Client.PostAsJsonAsync("/api/tasks", createRequest);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Act - Try to close from status 1
        var response = await Client.PutAsync($"/api/tasks/{createdTask!.Id}/close", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("can only be closed from the final status");
        errorContent.Should().Contain("3");
        errorContent.Should().Contain("Current status is 1");
    }

    [Fact]
    public async Task CloseTask_AlreadyClosed_ReturnsBadRequest()
    {
        // Arrange - Create, advance to final status, and close a task
        var createRequest = new CreateTaskRequest
        {
            TaskType = "Procurement",
            Title = "Purchase software licenses",
            AssignedUserId = 1
        };
        var createResponse = await Client.PostAsJsonAsync("/api/tasks", createRequest);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Move to final status
        await Client.PutAsJsonAsync($"/api/tasks/{createdTask!.Id}/status", new ChangeStatusRequest
        {
            TargetStatus = 2,
            AssignedUserId = 2,
            CustomData = new Dictionary<string, object>
            {
                { "priceQuote1", "Vendor 1" },
                { "priceQuote2", "Vendor 2" }
            }
        });

        await Client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}/status", new ChangeStatusRequest
        {
            TargetStatus = 3,
            AssignedUserId = 3,
            CustomData = new Dictionary<string, object> { { "receipt", "Receipt" } }
        });

        // Close the task
        await Client.PutAsync($"/api/tasks/{createdTask.Id}/close", null);

        // Act - Try to close again
        var response = await Client.PutAsync($"/api/tasks/{createdTask.Id}/close", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("already closed");
    }

    [Fact]
    public async Task ChangeStatus_ClosedTask_ReturnsBadRequest()
    {
        // Arrange - Create, advance to final status, and close a task
        var createRequest = new CreateTaskRequest
        {
            TaskType = "Development",
            Title = "Build search feature",
            AssignedUserId = 1
        };
        var createResponse = await Client.PostAsJsonAsync("/api/tasks", createRequest);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Move to final status
        await Client.PutAsJsonAsync($"/api/tasks/{createdTask!.Id}/status", new ChangeStatusRequest
        {
            TargetStatus = 2,
            AssignedUserId = 2,
            CustomData = new Dictionary<string, object> { { "specificationText", "Search spec" } }
        });

        await Client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}/status", new ChangeStatusRequest
        {
            TargetStatus = 3,
            AssignedUserId = 3,
            CustomData = new Dictionary<string, object> { { "branchName", "feature/search" } }
        });

        await Client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}/status", new ChangeStatusRequest
        {
            TargetStatus = 4,
            AssignedUserId = 4,
            CustomData = new Dictionary<string, object> { { "versionNumber", "v1.0.0" } }
        });

        // Close the task
        await Client.PutAsync($"/api/tasks/{createdTask.Id}/close", null);

        // Act - Try to change status of closed task
        var changeStatusRequest = new ChangeStatusRequest
        {
            TargetStatus = 3,
            AssignedUserId = 2,
            CustomData = new Dictionary<string, object>()
        };
        var response = await Client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}/status", changeStatusRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("Cannot change status of a closed task");
    }
}
