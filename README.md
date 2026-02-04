# PagueVeloz � Desafio T�cnico (Plataforma Transacional)

Resumo curto

PagueVeloz � uma plataforma de processamento de opera��es financeiras escrita em .NET 9, seguindo arquitetura limpa (Core/Application/Infrastructure/API), DDD e com foco em concorr�ncia, resili�ncia e observabilidade. Este reposit�rio cont�m a implementa��o das opera��es (cr�dito, d�bito, reserva, captura, libera��o, transfer�ncia, estorno), testes extensivos e infraestrutura m�nima (PostgreSQL + RabbitMQ via Docker Compose).

O que foi entregue (resumo para avaliador)

- Implementa��o das opera��es financeiras com regras de neg�cio no dom�nio (`PagueVeloz.Core`).
- Use Cases com idempot�ncia, locks por conta e persist�ncia via EF Core (`PagueVeloz.Application`).
- Reposit�rios EF Core e integra��o com PostgreSQL (`PagueVeloz.Infrastructure`).
- API REST com endpoints para todas as opera��es (`PagueVeloz.Api`).
- Retry exponencial na publica��o de eventos (RabbitMQ) e publisher substitu�vel por mock.
- Observabilidade: Serilog (console + arquivo), middlewares de logging, health checks e m�tricas simples.
- Seed robusto para avalia��o: v�rios clientes, m�ltiplas contas com saldos variados e hist�rico de opera��es.
- Testes: >50 testes unit�rios/integra��o incluindo concorr�ncia, idempot�ncia e edge cases (`PagueVeloz.Tests`).

Avisos r�pidos

- Logs gerados localmente em `PagueVeloz.Api/logs/` (est�o no `.gitignore`).

Instru��es detalhadas para clonagem e teste (passo-a-passo)

1) Requisitos na m�quina do avaliador

- Docker + Docker Compose
- .NET 9 SDK
- Git
- Navegador (para Swagger) e cliente HTTP (curl, httpie ou Postman)

2) Clonar o reposit�rio

```bash
git clone https://github.com/JJuniorS/PagueVeloz.git
cd PagueVeloz
```

3) Subir infraestrutura (PostgreSQL + RabbitMQ)

```bash
# Garantir que o docker esteja aberto
# do diret�rio raiz do reposit�rio
docker compose up -d
# verificar
docker ps
```

Containers esperados (exemplo):
```
postgres:16-alpine         Up
rabbitmq:3.11-management  Up
```

4) Restaurar depend�ncias (opcional, o `dotnet run` faz restore)

```bash
dotnet restore
```

5) Executar a aplica��o (API)

```bash
dotnet run --project PagueVeloz.Api
```

O `Program.cs` aplica migra��es automaticamente e executa um seed robusto se o banco estiver vazio. No console voc� ver� a sa�da do seed contendo `ClientId` e `AccountId` para testes; copie uma `AccountId` para usar com o Swagger ou curl.

Exemplo de sa�da do seed (parcial):

```
?? SEEDING DATABASE WITH TEST DATA
Client: Jo�o Silva
  AccountId: 1a2b3c4d-...
  Balance: $10000.00
  ...
? DATABASE SEEDING COMPLETED SUCCESSFULLY
```

6) Acessar Swagger UI

- Abrir: `https://localhost:7173/swagger` (porta pode variar, veja console)
- Use os endpoints dispon�veis para enviar requisi��es de teste.

7) Exemplos de chamadas (curl)

Creditar 100 na conta (exemplo):

```bash
curl -k -X POST https://localhost:7173/api/credit \
  -H "Content-Type: application/json" \
  -d '{"accountId":"<ACCOUNT_ID>","operationId":"<NEW_GUID>","amount":100.00}'
```

Debitar 50 na conta (exemplo):

```bash
curl -k -X POST https://localhost:7173/api/debit \
  -H "Content-Type: application/json" \
  -d '{"accountId":"<ACCOUNT_ID>","operationId":"<NEW_GUID>","amount":50.00}'
```

Transferir entre contas:

```bash
curl -k -X POST https://localhost:7173/api/transfer \
  -H "Content-Type: application/json" \
  -d '{"sourceAccountId":"<SRC_ID>","destinationAccountId":"<DST_ID>","operationId":"<NEW_GUID>","amount":100.00}'
```

8) Rodar testes automatizados (local)

```bash
dotnet test
```

- Todos os testes unit�rios e de infraestrutura est�o configurados em `PagueVeloz.Tests`.
- Os testes cobrem idempot�ncia, concorr�ncia (locks), edge-cases e fluxos principais.

9) Verificar dados 

Deixei uma controller "StartController" � um get que retorna os dados necess�rios para as requisi��es que desejarem fazer.

10) Ver mensagens no RabbitMQ (opcional)

- Acesse `http://localhost:15672` (user/password configurados no `docker-compose.yml`).
- Exchange principal: `pagueveloz.operations`

O que o avaliador deve procurar

- Endpoints funcionando no Swagger
- Logs estruturados no console e em `PagueVeloz.Api/logs` mostrando opera��es e eventos
- Seed populando m�ltiplos clientes/contas (facilita teste manual)
- Testes passando (`dotnet test`)
- Publica��o de eventos (se RabbitMQ habilitado) com retry em falhas
