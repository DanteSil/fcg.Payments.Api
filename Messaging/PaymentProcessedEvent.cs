namespace PaymentsAPI.Messaging;

internal sealed record PaymentProcessedEvent(Guid OrderId, Guid UserId, Guid GameId, decimal Price, string Status);
