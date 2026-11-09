using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PaymentsAPI.Messaging;

public sealed class OrderCreatedConsumer(
    IConfiguration configuration,
    ILogger<OrderCreatedConsumer> logger) : BackgroundService
{
    private const string QueueName = "pedidos.faturamento";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(configuration["RabbitMQ:Uri"]!),
            ClientProvidedName = "fcg-payments"
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            var payload = Encoding.UTF8.GetString(delivery.Body.Span);
            logger.LogInformation("Mensagem recebida com a chave {RoutingKey}: {Payload}", delivery.RoutingKey, payload);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken: delivery.CancellationToken);
        };

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        logger.LogInformation("Consumindo a fila {Queue}", QueueName);

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
