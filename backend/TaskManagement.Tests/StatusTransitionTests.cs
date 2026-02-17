using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskManagement.Application.DTOs;
using TaskManagement.Tests.Infrastructure;

namespace TaskManagement.Tests;

public class StatusTransitionTests : IntegrationTestBase
{
    public StatusTransitionTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ChangeStatus_ProcurementForward1To2WithValidData_Succeeds()
    {
        // Arrange - Create a Procurement task at status 1
        var createRequest = new CreateTaskRequest
        {
            TaskType = "Procurement",
            Title = "Purchase server hardware",
            AssignedUserId = 1
        };
        var createResponse = await Client.PostAsJsonAsync("/api/tasks", createRequest);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        var changeStatusRequest = new ChangeStatusRequest
        {
            TargetStatus = 2,
            AssignedUserId = 2,
            CustomData = new Dictionary<string, object>
            {
                { "priceQuote1", "Vendor A: $5000" },
                { "priceQuote2", "Vendor B: $4800" }
            }
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/tasks/{createdTask!.Id}/status", changeStatusRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedTask = await response.Content.ReadFromJsonAsync<TaskResponse>();
        updatedTask.Should().NotBeNull();
        updatedTask!.CurrentStatus.Should().Be(2);
        updatedTask.CurrentStatusLabel.Should().Be("Supplier offers received");
        updatedTask.AssignedUserId.Should().Be(2);
        updatedTask.AssignedUserName.Should().Be("Bob Smith");
        updatedTask.CustomData.Should().ContainKey("priceQuote1");
        updatedTask.CustomData.Should().ContainKey("priceQuote2");
        updatedTask.CustomData["priceQuote1"].ToString().Should().Be("Vendor A: $5000");
        updatedTask.CustomData["priceQuote2"].ToString().Should().Be("Vendor B: $4800");
    }

    [Fact]
    public async Task ChangeStatus_ProcurementForward1To2WithoutRequiredData_ReturnsBadRequest()
    {
        // Arrange - Create a Procurement task at status 1
        var createRequest = new CreateTaskRequest
        {
            TaskType = "Procurement",
            Title = "Purchase office furniture",
            AssignedUserId = 1
        };
        var createResponse = await Client.PostAsJsonAsync("/api/tasks", createRequest);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        var changeStatusRequest = new ChangeStatusRequest
        {
            TargetStatus = 2,
            AssignedUserId = 2,
            CustomData = new Dictionary<string, object>()
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/tasks/{createdTask!.Id}/status", changeStatusRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("Price Quote");
    }

    [Fact]
    public async Task ChangeStatus_SkipForwardStatus_ReturnsBadRequest()
    {
        // Arrange - Create a Development task at status 1
        var createRequest = new CreateTaskRequest
        {
            TaskType = "Development",
            Title = "Build payment gateway",
            AssignedUserId = 1
        };
        var createResponse = await Client.PostAsJsonAsync("/api/tasks", createRequest);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        var changeStatusRequest = new ChangeStatusRequest
        {
            TargetStatus = 3,
            AssignedUserId = 2,
            CustomData = new Dictionary<string, object>()
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/tasks/{createdTask!.Id}/status", changeStatusRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("sequential");
        errorContent.Should().Contain("Current status is 1");
    }

    [Fact]
    public async Task ChangeStatus_BackwardMove_Succeeds()
    {
        // Arrange - Create a Procurement task and advance it to status 3
        var createRequest = new CreateTaskRequest
        {
            TaskType = "Procurement",
            Title = "Purchase equipment",
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
                { "priceQuote1", "Quote A" },
                { "priceQuote2", "Quote B" }
            }
        });

        // Move 2 -> 3
        await Client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}/status", new ChangeStatusRequest
        {
            TargetStatus = 3,
            AssignedUserId = 3,
            CustomData = new Dictionary<string, object>
            {
                { "receipt", "Receipt #12345" }
            }
        });

        // Act - Move backward 3 -> 1
        var backwardRequest = new ChangeStatusRequest
        {
            TargetStatus = 1,
            AssignedUserId = 4,
            CustomData = new Dictionary<string, object>()
        };
        var response = await Client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}/status", backwardRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedTask = await response.Content.ReadFromJsonAsync<TaskResponse>();
        updatedTask.Should().NotBeNull();
        updatedTask!.CurrentStatus.Should().Be(1);
        updatedTask.AssignedUserId.Should().Be(4);
        updatedTask.AssignedUserName.Should().Be("Diana Prince");
    }

    [Fact]
    public async Task ChangeStatus_ForwardPastMaxStatus_ReturnsBadRequest()
    {
        // Arrange - Create Procurement task and advance to final status (3)
        var createRequest = new CreateTaskRequest
        {
            TaskType = "Procurement",
            Title = "Purchase licenses",
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
                { "priceQuote1", "Quote X" },
                { "priceQuote2", "Quote Y" }
            }
        });

        // Move 2 -> 3
        await Client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}/status", new ChangeStatusRequest
        {
            TargetStatus = 3,
            AssignedUserId = 3,
            CustomData = new Dictionary<string, object>
            {
                { "receipt", "Receipt ABC" }
            }
        });

        // Act - Try to move 3 -> 4 (beyond max status)
        var invalidRequest = new ChangeStatusRequest
        {
            TargetStatus = 4,
            AssignedUserId = 4,
            CustomData = new Dictionary<string, object>()
        };
        var response = await Client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}/status", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("exceeds the maximum status");
        errorContent.Should().Contain("3");
    }

    [Fact]
    public async Task ChangeStatus_DevelopmentSequentialAdvance_Succeeds()
    {
        // Arrange - Create Development task at status 1
        var createRequest = new CreateTaskRequest
        {
            TaskType = "Development",
            Title = "Build notification service",
            AssignedUserId = 1
        };
        var createResponse = await Client.PostAsJsonAsync("/api/tasks", createRequest);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Act & Assert - Move 1 -> 2 -> 3 -> 4
        var response1 = await Client.PutAsJsonAsync($"/api/tasks/{createdTask!.Id}/status", new ChangeStatusRequest
        {
            TargetStatus = 2,
            AssignedUserId = 2,
            CustomData = new Dictionary<string, object>
            {
                { "specificationText", "API spec for notifications" }
            }
        });
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        var task2 = await response1.Content.ReadFromJsonAsync<TaskResponse>();
        task2!.CurrentStatus.Should().Be(2);

        var response2 = await Client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}/status", new ChangeStatusRequest
        {
            TargetStatus = 3,
            AssignedUserId = 3,
            CustomData = new Dictionary<string, object>
            {
                { "branchName", "feature/notifications" }
            }
        });
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var task3 = await response2.Content.ReadFromJsonAsync<TaskResponse>();
        task3!.CurrentStatus.Should().Be(3);

        var response3 = await Client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}/status", new ChangeStatusRequest
        {
            TargetStatus = 4,
            AssignedUserId = 4,
            CustomData = new Dictionary<string, object>
            {
                { "versionNumber", "v1.2.0" }
            }
        });
        response3.StatusCode.Should().Be(HttpStatusCode.OK);
        var task4 = await response3.Content.ReadFromJsonAsync<TaskResponse>();
        task4!.CurrentStatus.Should().Be(4);
        task4.CurrentStatusLabel.Should().Be("Distribution completed");
        task4.CustomData.Should().ContainKey("specificationText");
        task4.CustomData.Should().ContainKey("branchName");
        task4.CustomData.Should().ContainKey("versionNumber");
    }
}
