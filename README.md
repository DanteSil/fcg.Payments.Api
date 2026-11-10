# PaymentsAPI

Host .NET 8 reservado para a simulação de pagamentos. Execute `dotnet run` neste diretório e acesse http://localhost:5103/health.

O perfil de desenvolvimento abre o Swagger em http://localhost:5103/swagger. As configurações padrão ficam em `appsettings.json`, com ajustes locais de log em `appsettings.Development.json`.

## Teste do consumidor RabbitMQ

O perfil de desenvolvimento usa o RabbitMQ local em `localhost:5672`, com as credenciais padrão `guest/guest` e o virtual host `/`. Com o contêiner rodando, pressione **F5** no Visual Studio ou execute no diretório do serviço:

```powershell
dotnet run
```

A fila `pedidos.faturamento` precisa existir antes de iniciar o serviço. Para usar outro broker, sobrescreva a URI com a variável de ambiente `RabbitMQ__Uri`.

Chame `POST /api/library/{gameId}` no catálogo com um JWT de usuário comum e anote o `orderId` da resposta. O console de Pagamentos deve mostrar o status para esse pedido. A simulação aprova preços maiores que zero e até 1000. Os demais são rejeitados.

No painel em http://localhost:15672, abra **Queues and Streams → pagamentos.catalogo → Get messages**. Selecione a opção de recolocar a mensagem na fila para inspecionar o `PaymentProcessedEvent` sem removê-lo. Confira `orderId`, `userId`, `gameId`, `price` e `status`.

O serviço cria a exchange durável `pagamentos` e vincula a fila durável `pagamentos.catalogo` pela chave `pagamento.processado`. Ele confirma o pedido original somente após o broker confirmar a publicação do resultado.
