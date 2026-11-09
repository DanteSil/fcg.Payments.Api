# PaymentsAPI

Host .NET 8 reservado para a simulação de pagamentos. Execute `dotnet run` neste diretório e acesse http://localhost:5103/health.

O perfil de desenvolvimento abre o Swagger em http://localhost:5103/swagger. As configurações padrão ficam em `appsettings.json`, com ajustes locais de log em `appsettings.Development.json`.

## Teste do consumidor RabbitMQ

O perfil de desenvolvimento usa o RabbitMQ local em `localhost:5672`, com as credenciais padrão `guest/guest` e o virtual host `/`. Com o contêiner rodando, pressione **F5** no Visual Studio ou execute no diretório do serviço:

```powershell
dotnet run
```

A fila `pedidos.faturamento` precisa existir antes de iniciar o serviço. Para usar outro broker, sobrescreva a URI com a variável de ambiente `RabbitMQ__Uri`.

No painel em http://localhost:15672, confirme que a fila mostra um consumidor. Em **Exchanges → pedidos → Publish message**, use a routing key `pedido.criado` e publique o payload `{"pedidoId":"teste-001"}`.

O console deve mostrar a chave e o payload. Depois do log, o serviço confirma a entrega com um ACK manual. Se a fila estiver vazia antes do teste, os contadores **Ready** e **Unacked** devem voltar a zero.
