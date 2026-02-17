using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskManagement.Application.DTOs;
using TaskManagement.Tests.Infrastructure;

namespace TaskManagement.Tests;

public class TaskCreationTests : IntegrationTestBase
{
    public TaskCreationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateTask_ProcurementWithValidUser_ReturnsCreatedTask()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            TaskType = "Procurement",
            Title = "Purchase new office laptops",
            AssignedUserId = 1
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();
        task.Should().NotBeNull();
        task!.Id.Should().BeGreaterThan(0);
        task.TaskType.Should().Be("Procurement");
        task.Title.Should().Be("Purchase new office laptops");
        task.CurrentStatus.Should().Be(1);
        task.CurrentStatusLabel.Should().Be("Created");
        task.IsClosed.Should().BeFalse();
        task.AssignedUserId.Should().Be(1);
        task.AssignedUserName.Should().Be("Alice Johnson");
        task.CustomData.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateTask_DevelopmentWithValidUser_ReturnsCreatedTask()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            TaskType = "Development",
            Title = "Implement user authentication module",
            AssignedUserId = 2
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();
        task.Should().NotBeNull();
        task!.Id.Should().BeGreaterThan(0);
        task.TaskType.Should().Be("Development");
        task.Title.Should().Be("Implement user authentication module");
        task.CurrentStatus.Should().Be(1);
        task.CurrentStatusLabel.Should().Be("Created");
        task.IsClosed.Should().BeFalse();
        task.AssignedUserId.Should().Be(2);
        task.AssignedUserName.Should().Be("Bob Smith");
        task.CustomData.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateTask_WithInvalidTaskType_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            TaskType = "Marketing",
            Title = "Launch product campaign",
            AssignedUserId = 1
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("Unknown task type");
        errorContent.Should().Contain("Marketing");
    }

    [Fact]
    public async Task CreateTask_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            TaskType = "Procurement",
            Title = "Purchase supplies",
            AssignedUserId = 9999
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("User with ID 9999 not found");
    }

    [Fact]
    public async Task CreateTask_WithoutAssignedUser_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            TaskType = "Development",
            Title = "Build API endpoints",
            AssignedUserId = 0
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("Assigned user is required");
    }
}
