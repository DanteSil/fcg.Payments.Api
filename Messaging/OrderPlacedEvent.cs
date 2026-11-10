namespace PaymentsAPI.Messaging;

internal sealed record OrderPlacedEvent(Guid OrderId, Guid UserId, Guid GameId, decimal Price);
