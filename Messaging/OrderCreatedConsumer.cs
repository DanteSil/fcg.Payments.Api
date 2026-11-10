using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PaymentsAPI.Messaging;

public sealed class OrderCreatedConsumer(
    IConfiguration configuration,
    ILogger<OrderCreatedConsumer> logger) : BackgroundService
{
    private const string QueueName = "pedidos.faturamento";
    private const string ResultExchange = "pagamentos";
    private const string ResultRoutingKey = "pagamento.processado";
    private const string CatalogQueue = "pagamentos.catalogo";
    private const decimal ApprovalLimit = 1000m;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(configuration["RabbitMQ:Uri"]!),
            ClientProvidedName = "fcg-payments"
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        var options = new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true);
        await using var publisher = await connection.CreateChannelAsync(options, stoppingToken);
        await publisher.ExchangeDeclareAsync(ResultExchange, ExchangeType.Direct, durable: true, cancellationToken: stoppingToken);
        await publisher.QueueDeclareAsync(CatalogQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await publisher.QueueBindAsync(CatalogQueue, ResultExchange, ResultRoutingKey, cancellationToken: stoppingToken);
        await channel.BasicQosAsync(0, 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            var cancellationToken = delivery.CancellationToken;
            OrderPlacedEvent? order;
            try
            {
                order = JsonSerializer.Deserialize<OrderPlacedEvent>(delivery.Body.Span, JsonOptions);
            }
            catch (JsonException)
            {
                order = null;
            }

            if (order is null || order.OrderId == Guid.Empty || order.UserId == Guid.Empty || order.GameId == Guid.Empty)
            {
                logger.LogWarning("Pedido inválido recebido em {Queue}", QueueName);
                await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, cancellationToken: cancellationToken);
                return;
            }

            var status = order.Price is > 0 and <= ApprovalLimit ? "Approved" : "Rejected";
            var result = new PaymentProcessedEvent(order.OrderId, order.UserId, order.GameId, order.Price, status);
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                Persistent = true,
                MessageId = order.OrderId.ToString(),
                Type = nameof(PaymentProcessedEvent)
            };

            try
            {
                var body = JsonSerializer.SerializeToUtf8Bytes(result, JsonOptions);
                await publisher.BasicPublishAsync(ResultExchange, ResultRoutingKey, mandatory: true,
                    basicProperties: properties, body: body, cancellationToken: cancellationToken);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Falha ao publicar o resultado do pedido {OrderId}", order.OrderId);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, cancellationToken: cancellationToken);
                return;
            }

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            logger.LogInformation("Pagamento do pedido {OrderId} processado com status {Status}", order.OrderId, status);
        };

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        logger.LogInformation("Consumindo a fila {Queue}", QueueName);

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
