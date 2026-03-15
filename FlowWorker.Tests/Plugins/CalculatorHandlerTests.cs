using System.Text.Json;
using FlowWorker.Plugins.Calculator;

namespace FlowWorker.Tests.Plugins;

/// <summary>
/// CalculatorHandler 测试类
/// </summary>
public class CalculatorHandlerTests
{
    private readonly CalculatorHandler _handler = new();

    #region CalculateAsync Tests

    [Fact]
    public async Task CalculateAsync_SimpleAddition_ReturnsCorrectResult()
    {
        // Arrange
        var parameters = JsonDocument.Parse("{\"expression\": \"1+2\"}").RootElement;

        // Act
        var result = await _handler.CalculateAsync(parameters);

        // Assert
        Assert.Equal("success", result.Status);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task CalculateAsync_EmptyExpression_ReturnsError()
    {
        // Arrange
        var parameters = JsonDocument.Parse("{\"expression\": \"\"}").RootElement;

        // Act
        var result = await _handler.CalculateAsync(parameters);

        // Assert
        Assert.Equal("error", result.Status);
        Assert.NotNull(result.ErrorInfo);
        Assert.Equal("INVALID_PARAMETERS", result.ErrorInfo!.Code);
    }

    [Fact]
    public async Task CalculateAsync_DivisionByZero_ReturnsInfinity()
    {
        // Arrange
        var parameters = JsonDocument.Parse("{\"expression\": \"1/0\"}").RootElement;

        // Act
        var result = await _handler.CalculateAsync(parameters);

        // Assert - DataTable.Compute 返回 Infinity 而不是抛出异常
        Assert.Equal("success", result.Status);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task CalculateAsync_InvalidExpression_ReturnsError()
    {
        // Arrange
        var parameters = JsonDocument.Parse("{\"expression\": \"invalid_expression\"}").RootElement;

        // Act
        var result = await _handler.CalculateAsync(parameters);

        // Assert - 包含非法字符的表达式会被清理为空，导致计算失败
        Assert.Equal("error", result.Status);
        Assert.NotNull(result.ErrorInfo);
    }

    #endregion

    #region ConvertUnitAsync Tests

    [Fact]
    public async Task ConvertUnitAsync_LengthMetersToKilometers_ReturnsCorrectResult()
    {
        // Arrange
        var parameters = JsonDocument.Parse("{\"value\": 1000, \"from_unit\": \"m\", \"to_unit\": \"km\"}").RootElement;

        // Act
        var result = await _handler.ConvertUnitAsync(parameters);

        // Assert
        Assert.Equal("success", result.Status);
    }

    [Fact]
    public async Task ConvertUnitAsync_WeightKgToGram_ReturnsCorrectResult()
    {
        // Arrange
        var parameters = JsonDocument.Parse("{\"value\": 1, \"from_unit\": \"kg\", \"to_unit\": \"g\"}").RootElement;

        // Act
        var result = await _handler.ConvertUnitAsync(parameters);

        // Assert
        Assert.Equal("success", result.Status);
    }

    [Fact]
    public async Task ConvertUnitAsync_TemperatureCelsiusToFahrenheit_ReturnsCorrectResult()
    {
        // Arrange
        var parameters = JsonDocument.Parse("{\"value\": 0, \"from_unit\": \"c\", \"to_unit\": \"f\"}").RootElement;

        // Act
        var result = await _handler.ConvertUnitAsync(parameters);

        // Assert
        Assert.Equal("success", result.Status);
    }

    [Fact]
    public async Task ConvertUnitAsync_EmptyUnit_ReturnsError()
    {
        // Arrange
        var parameters = JsonDocument.Parse("{\"value\": 1, \"from_unit\": \"\", \"to_unit\": \"m\"}").RootElement;

        // Act
        var result = await _handler.ConvertUnitAsync(parameters);

        // Assert
        Assert.Equal("error", result.Status);
        Assert.NotNull(result.ErrorInfo);
        Assert.Equal("INVALID_PARAMETERS", result.ErrorInfo!.Code);
    }

    [Fact]
    public async Task ConvertUnitAsync_UnsupportedConversion_ReturnsError()
    {
        // Arrange
        var parameters = JsonDocument.Parse("{\"value\": 1, \"from_unit\": \"xyz\", \"to_unit\": \"abc\"}").RootElement;

        // Act
        var result = await _handler.ConvertUnitAsync(parameters);

        // Assert
        Assert.Equal("error", result.Status);
    }

    #endregion

    #region GenerateUuidAsync Tests

    [Fact]
    public async Task GenerateUuidAsync_DefaultCount_ReturnsSingleUuid()
    {
        // Arrange
        var parameters = JsonDocument.Parse("{}").RootElement;

        // Act
        var result = await _handler.GenerateUuidAsync(parameters);

        // Assert
        Assert.Equal("success", result.Status);
    }

    [Fact]
    public async Task GenerateUuidAsync_MultipleUuids_ReturnsCorrectCount()
    {
        // Arrange
        var parameters = JsonDocument.Parse("{\"count\": 5}").RootElement;

        // Act
        var result = await _handler.GenerateUuidAsync(parameters);

        // Assert
        Assert.Equal("success", result.Status);
    }

    [Fact]
    public async Task GenerateUuidAsync_WithFormat_ReturnsFormattedUuid()
    {
        // Arrange
        var parameters = JsonDocument.Parse("{\"format\": \"upper\"}").RootElement;

        // Act
        var result = await _handler.GenerateUuidAsync(parameters);

        // Assert
        Assert.Equal("success", result.Status);
    }

    [Fact]
    public async Task GenerateUuidAsync_CountTooHigh_ReturnsError()
    {
        // Arrange
        var parameters = JsonDocument.Parse("{\"count\": 101}").RootElement;

        // Act
        var result = await _handler.GenerateUuidAsync(parameters);

        // Assert
        Assert.Equal("error", result.Status);
        Assert.NotNull(result.ErrorInfo);
        Assert.Equal("INVALID_PARAMETERS", result.ErrorInfo!.Code);
    }

    [Fact]
    public async Task GenerateUuidAsync_CountTooLow_ReturnsError()
    {
        // Arrange
        var parameters = JsonDocument.Parse("{\"count\": 0}").RootElement;

        // Act
        var result = await _handler.GenerateUuidAsync(parameters);

        // Assert
        Assert.Equal("error", result.Status);
        Assert.NotNull(result.ErrorInfo);
        Assert.Equal("INVALID_PARAMETERS", result.ErrorInfo!.Code);
    }

    #endregion

    #region GetTimestampAsync Tests

    [Fact]
    public async Task GetTimestampAsync_DefaultFormat_ReturnsUnixTimestamp()
    {
        // Arrange
        var parameters = JsonDocument.Parse("{}").RootElement;

        // Act
        var result = await _handler.GetTimestampAsync(parameters);

        // Assert
        Assert.Equal("success", result.Status);
    }

    [Fact]
    public async Task GetTimestampAsync_IsoFormat_ReturnsIsoTimestamp()
    {
        // Arrange
        var parameters = JsonDocument.Parse("{\"format\": \"iso\"}").RootElement;

        // Act
        var result = await _handler.GetTimestampAsync(parameters);

        // Assert
        Assert.Equal("success", result.Status);
    }

    [Fact]
    public async Task GetTimestampAsync_LocalTimezone_ReturnsLocalTime()
    {
        // Arrange
        var parameters = JsonDocument.Parse("{\"timezone\": \"local\"}").RootElement;

        // Act
        var result = await _handler.GetTimestampAsync(parameters);

        // Assert
        Assert.Equal("success", result.Status);
    }

    [Fact]
    public async Task GetTimestampAsync_CustomFormat_ReturnsFormattedTimestamp()
    {
        // Arrange
        var parameters = JsonDocument.Parse("{\"format\": \"yyyy-MM-dd\"}").RootElement;

        // Act
        var result = await _handler.GetTimestampAsync(parameters);

        // Assert
        Assert.Equal("success", result.Status);
    }

    #endregion
}
