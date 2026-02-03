# PagueVeloz — Desafio Técnico (Plataforma Transacional)

Resumo curto

PagueVeloz é uma plataforma de processamento de operações financeiras escrita em .NET 9, seguindo arquitetura limpa (Core/Application/Infrastructure/API), DDD e com foco em concorrência, resiliência e observabilidade. Este repositório contém a implementação das operações (crédito, débito, reserva, captura, liberação, transferência, estorno), testes extensivos e infraestrutura mínima (PostgreSQL + RabbitMQ via Docker Compose).

O que foi entregue (resumo para avaliador)

- Implementação das operações financeiras com regras de negócio no domínio (`PagueVeloz.Core`).
- Use Cases com idempotência, locks por conta e persistência via EF Core (`PagueVeloz.Application`).
- Repositórios EF Core e integração com PostgreSQL (`PagueVeloz.Infrastructure`).
- API REST com endpoints para todas as operações (`PagueVeloz.Api`).
- Retry exponencial na publicação de eventos (RabbitMQ) e publisher substituível por mock.
- Observabilidade: Serilog (console + arquivo), middlewares de logging, health checks e métricas simples.
- Seed robusto para avaliação: vários clientes, múltiplas contas com saldos variados e histórico de operações.
- Testes: >50 testes unitários/integração incluindo concorrência, idempotência e edge cases (`PagueVeloz.Tests`).

Avisos rápidos

- Após subir a aplicação, acessar localhost:{porta}/swagger para acessar os endpoints.
- Logs gerados localmente em `PagueVeloz.Api/logs/` (estão no `.gitignore`).
- Não há segredos hard-coded no código; variáveis sensíveis devem ser configuradas via `appsettings` ou `.env` fora do repositório.

Instruções detalhadas para clonagem e teste (passo-a-passo)

1) Requisitos na máquina do avaliador

- Docker + Docker Compose
- .NET 9 SDK
- Git
- Navegador (para Swagger) e cliente HTTP (curl, httpie ou Postman)

2) Clonar o repositório

```bash
git clone https://github.com/JJuniorS/PagueVeloz.git
cd PagueVeloz
git checkout dev
```

3) Subir infraestrutura (PostgreSQL + RabbitMQ)

```bash
# do diretório raiz do repositório
docker compose up -d
# verificar
docker ps
```

Containers esperados (exemplo):
```
postgres:16-alpine         Up
rabbitmq:3.11-management  Up
```

4) Restaurar dependências (opcional, o `dotnet run` faz restore)

```bash
dotnet restore
```

5) Executar a aplicação (API)

```bash
dotnet run --project PagueVeloz.Api
```

O `Program.cs` aplica migrações automaticamente e executa um seed robusto se o banco estiver vazio. No console você verá a saída do seed contendo `ClientId` e `AccountId` para testes; copie uma `AccountId` para usar com o Swagger ou curl.

Exemplo de saída do seed (parcial):

```
?? SEEDING DATABASE WITH TEST DATA
Client: João Silva
  AccountId: 1a2b3c4d-...
  Balance: $10000.00
  ...
? DATABASE SEEDING COMPLETED SUCCESSFULLY
```

6) Acessar Swagger UI

- Abrir: `https://localhost:7173/swagger` (porta pode variar, veja console)
- Use os endpoints disponíveis para enviar requisições de teste.

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

- Todos os testes unitários e de infraestrutura estão configurados em `PagueVeloz.Tests`.
- Os testes cobrem idempotência, concorrência (locks), edge-cases e fluxos principais.

9) Verificar dados 

Deixei uma controller "StartController" é um get que retorna os dados necessários para as requisições que desejarem fazer.

10) Ver mensagens no RabbitMQ (opcional)

- Acesse `http://localhost:15672` (user/password configurados no `docker-compose.yml`).
- Exchange principal: `pagueveloz.operations`

O que o avaliador deve procurar

- Endpoints funcionando no Swagger
- Logs estruturados no console e em `PagueVeloz.Api/logs` mostrando operações e eventos
- Seed populando múltiplos clientes/contas (facilita teste manual)
- Testes passando (`dotnet test`)
- Publicação de eventos (se RabbitMQ habilitado) com retry em falhas
