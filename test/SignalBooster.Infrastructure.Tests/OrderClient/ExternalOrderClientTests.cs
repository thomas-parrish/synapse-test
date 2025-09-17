using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using SignalBooster.Domain;
using SignalBooster.Domain.Prescriptions;
using SignalBooster.Infrastructure.OrderClient;
using System.Net;

namespace SignalBooster.Infrastructure.Tests.OrderClient;

public class ExternalOrderClientTests
{
    private readonly ExternalOrderRequestFormatter _formatter = new();

    private static PhysicianNote MakeNote() => new()
    {
        PatientName = "Test",
        PatientDateOfBirth = new DateOnly(1990, 1, 1),
        Diagnosis = "OSA",
        OrderingPhysician = "Dr. Who",
        Prescription = new CpapPrescription(
            MaskType: MaskType.FullFace,
            HeatedHumidifier: true,
            Ahi: 20)
    };

    private ExternalOrderClient CreateClient(Mock<HttpMessageHandler> handlerMock)
    {
        var http = new HttpClient(handlerMock.Object);
        return new ExternalOrderClient(http, _formatter, NullLogger<ExternalOrderClient>.Instance);
    }

    [Fact]
    public async Task SendAsync_ReturnsTrue_OnSuccess()
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var client = CreateClient(handlerMock);

        var result = await client.SendAsync(MakeNote(), new Uri("https://fake/endpoint"));

        Assert.True(result);

        // verify we did a POST
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri == new Uri("https://fake/endpoint")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_ReturnsFalse_OnFailureStatus()
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

        var client = CreateClient(handlerMock);

        var result = await client.SendAsync(MakeNote(), new Uri("https://fake/endpoint"));

        Assert.False(result);
    }

    [Fact]
    public async Task SendAsync_ReturnsFalse_OnException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("boom"));

        var client = CreateClient(handlerMock);

        var result = await client.SendAsync(MakeNote(), new Uri("https://fake/endpoint"));

        Assert.False(result);
    }

    [Fact]
    public async Task SendAsync_ReturnsFalse_OnCancellation()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException("simulated cancel"));

        var http = new HttpClient(handlerMock.Object);

        var sut = CreateClient(handlerMock);

        var note = new PhysicianNote
        {
            PatientName = "Test",
            PatientDateOfBirth = new DateOnly(2000, 1, 1),
            Diagnosis = "Unit test",
            OrderingPhysician = "Dr. Cancel",
            Prescription = new OxygenPrescription(2, UsageContext.Sleep)
        };

        // Act
        var result = await sut.SendAsync(note, new Uri("http://fake/endpoint"));

        // Assert
        Assert.False(result);
    }


}