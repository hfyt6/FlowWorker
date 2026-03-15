using FlowWorker.Core.DTOs;
using FlowWorker.Core.Repositories;
using FlowWorker.Core.Services;
using FlowWorker.Shared.Entities;
using FlowWorker.Shared.Enums;
using Moq;
using Xunit;

namespace FlowWorker.Tests.Core;

/// <summary>
/// SessionService 测试类
/// </summary>
public class SessionServiceTests
{
    private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly Mock<IMessageRepository> _messageRepositoryMock;
    private readonly Mock<IApiConfigRepository> _apiConfigRepositoryMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly SessionService _sessionService;

    public SessionServiceTests()
    {
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _messageRepositoryMock = new Mock<IMessageRepository>();
        _apiConfigRepositoryMock = new Mock<IApiConfigRepository>();
        _memberRepositoryMock = new Mock<IMemberRepository>();

        _sessionService = new SessionService(
            _sessionRepositoryMock.Object,
            _messageRepositoryMock.Object,
            _apiConfigRepositoryMock.Object,
            _memberRepositoryMock.Object);
    }

    #region CreateSessionAsync Tests

    [Fact]
    public async Task CreateSessionAsync_WithValidRequest_CreatesSessionWithWorkingDirectory()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var apiConfigId = Guid.NewGuid();
        var member = new Member
        {
            Id = memberId,
            Name = "Test AI",
            Type = MemberType.AI,
            ApiConfigId = apiConfigId,
            Model = "gpt-4"
        };

        _memberRepositoryMock.Setup(x => x.GetByIdAsync(memberId))
            .ReturnsAsync(member);

        _sessionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Session>()))
            .Returns(Task.CompletedTask);

        _sessionRepositoryMock.Setup(x => x.AddMemberAsync(It.IsAny<Guid>(), memberId))
            .Returns(Task.CompletedTask);

        var request = new CreateSessionRequest
        {
            Title = "Test Session",
            MemberId = memberId
        };

        // Act
        var sessionId = await _sessionService.CreateSessionAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, sessionId);
        _sessionRepositoryMock.Verify(x => x.AddAsync(It.Is<Session>(s =>
            s.Title == "Test Session" &&
            s.Type == SessionType.Single &&
            !string.IsNullOrEmpty(s.WorkingDirectory))), Times.Once);
    }

    [Fact]
    public async Task CreateSessionAsync_WithCustomWorkingDirectory_UsesProvidedPath()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var apiConfigId = Guid.NewGuid();
        var customPath = Path.Combine(Path.GetTempPath(), "CustomTestPath");

        var member = new Member
        {
            Id = memberId,
            Name = "Test AI",
            Type = MemberType.AI,
            ApiConfigId = apiConfigId,
            Model = "gpt-4"
        };

        _memberRepositoryMock.Setup(x => x.GetByIdAsync(memberId))
            .ReturnsAsync(member);

        _sessionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Session>()))
            .Returns(Task.CompletedTask);

        _sessionRepositoryMock.Setup(x => x.AddMemberAsync(It.IsAny<Guid>(), memberId))
            .Returns(Task.CompletedTask);

        var request = new CreateSessionRequest
        {
            Title = "Test Session",
            MemberId = memberId,
            WorkingDirectory = customPath
        };

        // Act
        var sessionId = await _sessionService.CreateSessionAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, sessionId);
        _sessionRepositoryMock.Verify(x => x.AddAsync(It.Is<Session>(s =>
            s.WorkingDirectory == Path.GetFullPath(customPath))), Times.Once);

        // Cleanup
        if (Directory.Exists(customPath))
        {
            Directory.Delete(customPath, true);
        }
    }

    [Fact]
    public async Task CreateSessionAsync_WithNonExistentMember_ThrowsInvalidOperationException()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        _memberRepositoryMock.Setup(x => x.GetByIdAsync(memberId))
            .ReturnsAsync((Member?)null);

        var request = new CreateSessionRequest
        {
            Title = "Test Session",
            MemberId = memberId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sessionService.CreateSessionAsync(request));
        Assert.Contains("不存在", exception.Message);
    }

    [Fact]
    public async Task CreateSessionAsync_WithUserMember_ThrowsInvalidOperationException()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var member = new Member
        {
            Id = memberId,
            Name = "Test User",
            Type = MemberType.User
        };

        _memberRepositoryMock.Setup(x => x.GetByIdAsync(memberId))
            .ReturnsAsync(member);

        var request = new CreateSessionRequest
        {
            Title = "Test Session",
            MemberId = memberId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sessionService.CreateSessionAsync(request));
        Assert.Contains("单聊只能选择AI成员", exception.Message);
    }

    [Fact]
    public async Task CreateSessionAsync_WithMemberWithoutApiConfig_ThrowsInvalidOperationException()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var member = new Member
        {
            Id = memberId,
            Name = "Test AI",
            Type = MemberType.AI,
            ApiConfigId = null
        };

        _memberRepositoryMock.Setup(x => x.GetByIdAsync(memberId))
            .ReturnsAsync(member);

        var request = new CreateSessionRequest
        {
            Title = "Test Session",
            MemberId = memberId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sessionService.CreateSessionAsync(request));
        Assert.Contains("没有关联的API配置", exception.Message);
    }

    #endregion

    #region CreateGroupSessionAsync Tests

    [Fact]
    public async Task CreateGroupSessionAsync_WithValidRequest_CreatesGroupSessionWithWorkingDirectory()
    {
        // Arrange
        var creatorId = Guid.NewGuid();
        var aiMemberId = Guid.NewGuid();

        var creator = new Member
        {
            Id = creatorId,
            Name = "Test User",
            Type = MemberType.User
        };

        var aiMember = new Member
        {
            Id = aiMemberId,
            Name = "Test AI",
            Type = MemberType.AI
        };

        _memberRepositoryMock.Setup(x => x.GetByIdAsync(creatorId))
            .ReturnsAsync(creator);

        _memberRepositoryMock.Setup(x => x.GetByIdAsync(aiMemberId))
            .ReturnsAsync(aiMember);

        _sessionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Session>()))
            .Returns(Task.CompletedTask);

        _sessionRepositoryMock.Setup(x => x.AddMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        var request = new CreateGroupSessionRequest
        {
            Title = "Test Group Session",
            CreatedBy = creatorId,
            AiMemberIds = new List<Guid> { aiMemberId }
        };

        // Act
        var sessionId = await _sessionService.CreateGroupSessionAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, sessionId);
        _sessionRepositoryMock.Verify(x => x.AddAsync(It.Is<Session>(s =>
            s.Title == "Test Group Session" &&
            s.Type == SessionType.Group &&
            !string.IsNullOrEmpty(s.WorkingDirectory))), Times.Once);
    }

    [Fact]
    public async Task CreateGroupSessionAsync_WithoutAiMembers_ThrowsInvalidOperationException()
    {
        // Arrange
        var creatorId = Guid.NewGuid();
        var creator = new Member
        {
            Id = creatorId,
            Name = "Test User",
            Type = MemberType.User
        };

        _memberRepositoryMock.Setup(x => x.GetByIdAsync(creatorId))
            .ReturnsAsync(creator);

        var request = new CreateGroupSessionRequest
        {
            Title = "Test Group Session",
            CreatedBy = creatorId,
            AiMemberIds = new List<Guid>()
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sessionService.CreateGroupSessionAsync(request));
        Assert.Contains("至少需要选择一个 AI 成员", exception.Message);
    }

    #endregion

    #region GetSessionDetailAsync Tests

    [Fact]
    public async Task GetSessionDetailAsync_WithExistingSession_ReturnsDetailWithWorkingDirectory()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var workingDir = Path.Combine(Path.GetTempPath(), "TestSession");

        var session = new Session
        {
            Id = sessionId,
            Title = "Test Session",
            Type = SessionType.Single,
            WorkingDirectory = workingDir,
            SessionMembers = new List<SessionMember>()
        };

        _sessionRepositoryMock.Setup(x => x.GetSessionWithMembersAsync(sessionId))
            .ReturnsAsync(session);

        _messageRepositoryMock.Setup(x => x.GetBySessionIdAsync(sessionId))
            .ReturnsAsync(new List<Message>());

        // Act
        var result = await _sessionService.GetSessionDetailAsync(sessionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sessionId, result.Id);
        Assert.Equal(workingDir, result.WorkingDirectory);
    }

    [Fact]
    public async Task GetSessionDetailAsync_WithNonExistentSession_ReturnsNull()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _sessionRepositoryMock.Setup(x => x.GetSessionWithMembersAsync(sessionId))
            .ReturnsAsync((Session?)null);

        // Act
        var result = await _sessionService.GetSessionDetailAsync(sessionId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region DeleteSessionAsync Tests

    [Fact]
    public async Task DeleteSessionAsync_WithExistingSession_DeletesSessionAndMessages()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new Session
        {
            Id = sessionId,
            Title = "Test Session"
        };

        _sessionRepositoryMock.Setup(x => x.GetByIdAsync(sessionId))
            .ReturnsAsync(session);

        _messageRepositoryMock.Setup(x => x.DeleteBySessionIdAsync(sessionId))
            .Returns(Task.CompletedTask);

        _sessionRepositoryMock.Setup(x => x.DeleteAsync(session))
            .Returns(Task.CompletedTask);

        // Act
        await _sessionService.DeleteSessionAsync(sessionId);

        // Assert
        _messageRepositoryMock.Verify(x => x.DeleteBySessionIdAsync(sessionId), Times.Once);
        _sessionRepositoryMock.Verify(x => x.DeleteAsync(session), Times.Once);
    }

    [Fact]
    public async Task DeleteSessionAsync_WithNonExistentSession_ThrowsInvalidOperationException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _sessionRepositoryMock.Setup(x => x.GetByIdAsync(sessionId))
            .ReturnsAsync((Session?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sessionService.DeleteSessionAsync(sessionId));
        Assert.Contains("不存在", exception.Message);
    }

    #endregion
}
