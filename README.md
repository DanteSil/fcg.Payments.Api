# PaymentsAPI

Serviço .NET 8 que consome pedidos e simula pagamentos via RabbitMQ. Para execução direta, instale .NET 8 SDK, disponibilize o broker, execute `dotnet run` neste diretório e acesse http://localhost:5103/health.

O perfil de desenvolvimento abre o Swagger em http://localhost:5103/swagger. As configurações padrão ficam em `appsettings.json`, com ajustes locais de log em `appsettings.Development.json`.

## Teste do consumidor RabbitMQ

O perfil de desenvolvimento usa o RabbitMQ local em `localhost:5672`, com as credenciais padrão `guest/guest` e o virtual host `/`. Com o contêiner rodando, pressione **F5** no Visual Studio ou execute no diretório do serviço:

```powershell
dotnet run
```

A fila `pedidos.faturamento` precisa existir antes de iniciar o serviço, ou a declaração da topologia deve ser habilitada. Para criar a exchange, fila e vínculo automaticamente e configurar outro broker, execute antes de `dotnet run`:

```powershell
$env:RabbitMQ__Uri = 'amqp://guest:guest@localhost:5672/%2f'
$env:RabbitMQ__DeclareOrdersTopology = 'true'
```

Ajuste a URI às credenciais do ambiente. Compose e Kubernetes já habilitam a declaração da topologia de pedidos e usam o usuário `fcg` com a senha definida no `.env` da infraestrutura.

Chame `POST /api/library/{gameId}` no catálogo com um JWT de usuário comum e anote o `orderId` da resposta. O console de Pagamentos deve mostrar o status para esse pedido. A simulação aprova preços maiores que zero e até 1000. Os demais são rejeitados.

Confira o resultado nos logs de Pagamentos e a inclusão do jogo em `GET /api/library` no Catálogo. A fila `pagamentos.catalogo` pode estar vazia após o teste porque o Catálogo consome os resultados automaticamente. O painel de um broker local padrão fica em http://localhost:15672; no Compose ou no encaminhamento indicado pela infraestrutura, use http://localhost:15673.

O serviço cria a exchange durável `pagamentos` e vincula a fila durável `pagamentos.catalogo` pela chave `pagamento.processado`. Ele confirma o pedido original somente após o broker confirmar a publicação do resultado.

## Kubernetes

Os manifestos deste serviço ficam em k8s/. O script de implantação, a infraestrutura e as instruções para clonar os cinco repositórios estão em https://github.com/DanteSil/fcg.Infrastructure.
