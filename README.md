# PagueVeloz - Plataforma Transacional de Adquirência

**PagueVeloz** é uma plataforma de processamento de operações financeiras construída com .NET 9, seguindo princípios de arquitetura limpa, DDD e microsserviços. O sistema gerencia clientes, contas e operações financeiras em ambiente distribuído com alta concorrência e resiliência.

---

## ?? Sumário

- [Visão Geral](#visão-geral)
- [Requisitos do Desafio](#requisitos-do-desafio)
- [Arquitetura](#arquitetura)
- [Setup Local](#setup-local)
- [Testes](#testes)
- [API Endpoints](#api-endpoints)
- [Diferenciais Implementados](#diferenciais-implementados)
- [Troubleshooting](#troubleshooting)

---

## ?? Visão Geral

### Objetivos Principais
- ? Processar múltiplas operações financeiras (Crédito, Débito, Reserva, Captura, Liberação, Transferência, Estorno)
- ? Suportar múltiplas contas por cliente
- ? Implementar limite de crédito operacional
- ? Garantir transações reversíveis
- ? Processamento totalmente assíncrono
- ? Resiliência a falhas com retry exponencial
- ? Controle de concorrência com locks
- ? Persistência com PostgreSQL
- ? Publicação de eventos via RabbitMQ
- ? Idempotência de operações

---

## ? Requisitos do Desafio

### Regras de Negócio Implementadas

| Requisito | Status | Detalhes |
|-----------|--------|---------|
| Múltiplas contas por cliente | ? | Foreign Key Client?Accounts |
| Limite de crédito operacional | ? | Account com Balance + ReservedBalance + CreditLimit |
| Transações reversíveis | ? | RevertUseCase implementado |
| Processamento assíncrono | ? | Todos os Use Cases async/await |
| Resiliência e retry | ? | RabbitMQ com backoff exponencial (200ms ? 400ms ? 800ms) |
| Idempotência | ? | OperationId deduplicado por conta |
| Locks e concorrência | ? | AccountLockManager sincroniza operações |
| Histórico e auditoria | ? | Todas as operações persistidas com timestamps |
| Rollback em falhas | ? | Transações ACID via EF Core |

### Requisitos Técnicos Avaliados

| Requisito | Status | Implementado em |
|-----------|--------|---------|
| Assincronia (async/await) | ? | Use Cases, Repositories, Controllers |
| Uso eficiente de memória | ? | AsNoTracking em queries, Scoped DI |
| SOLID/OOP | ? | Interfaces segregadas, Single Responsibility |
| Padrões (DDD, Clean, Onion) | ? | Core, Application, Infrastructure, API layers |
| Código testável | ? | Use Cases isolados, mockável via interfaces |
| Arquitetura escalável | ? | Preparado para divisão em microsserviços |
| Retry com fallback | ? | Exponential backoff no RabbitMQ |
| Transações distribuídas | ? | EF Core + PostgreSQL ACID |
| Modelagem relacional | ? | ClientEntity, AccountEntity, OperationEntity |

---

## ??? Arquitetura

```
PagueVeloz/
??? PagueVeloz.Core/               # Domínio puro
?   ??? Entities/                  # Account, Operation, Client
?   ??? Enums/                     # EOperationType, EOperationStatus, EAccountStatus
?   ??? ValueObjects/              # (extensível)
?
??? PagueVeloz.Application/        # Use Cases & DTOs
?   ??? UseCases/                  # DebitUseCase, CreditUseCase, ...
?   ??? DTOs/                      # DebitRequest, CreditRequest, ...
?   ??? Events/                    # OperationCreatedEvent
?   ??? Interfaces/                # IAccountRepository, IEventPublisher, ...
?
??? PagueVeloz.Infrastructure/     # Implementações externas
?   ??? Persistence/               # PagueVelozDbContext, Migrations
?   ??? Entities/                  # ClientEntity, AccountEntity, OperationEntity (EF)
?   ??? Repositories/              # EfCoreAccountRepository, EfCoreOperationRepository
?   ??? Messaging/                 # RabbitMqEventPublisher, ServiceCollectionExtensions
?   ??? Locks/                     # AccountLockManager
?   ??? Persistence.Entities/      # Mapeamento de dados
?
??? PagueVeloz.Api/                # API REST
?   ??? Controllers/               # DebitController, CreditController, ...
?   ??? Program.cs                 # DI Registration, Middleware
?   ??? appsettings.json           # Configuration
?   ??? desafio.cs                 # Requisitos do desafio
?
??? docker-compose.yml             # PostgreSQL + RabbitMQ
```

### Fluxo de Operação

```
1. Requisição chega no Controller
   ?
2. Controller instancia o Use Case apropriado
   ?
3. Use Case adquire Lock na Account (AccountLockManager)
   ?
4. Use Case verifica Idempotência (OperationId já existe?)
   ?
5. Use Case executa regra de negócio (Account.Debit/Credit/...)
   ?
6. Use Case persiste Account atualizada (EF Core)
   ?
7. Use Case persiste Operation (auditoria)
   ?
8. Use Case publica evento no RabbitMQ (com retry exponencial)
   ?
9. Lock é liberado
   ?
10. Resposta retorna ao cliente
```

---

## ?? Setup Local

### Pré-requisitos
- **Docker** e **Docker Compose**
- **.NET 9 SDK**
- **Git**

### Passo 1: Clone o Repositório

```bash
git clone https://github.com/JJuniorS/PagueVeloz.git
cd PagueVeloz
git checkout dev
```

### Passo 2: Suba os Containers (PostgreSQL + RabbitMQ)

```bash
docker compose up -d
```

Verifique se estão rodando:
```bash
docker ps
```

Esperado:
```
CONTAINER ID   IMAGE                      STATUS
xxxxx          postgres:16-alpine         Up 2 minutes
xxxxx          rabbitmq:3.11-management  Up 2 minutes
```

### Passo 3: Restaure Dependências

```bash
dotnet restore
```

### Passo 4: Rode a Aplicação

```bash
dotnet run --project PagueVeloz.Api
```

Esperado:
```
info: Program
Seed ClientId: 12345678-1234-1234-1234-123456789012
Seed AccountId: 87654321-4321-4321-4321-210987654321
info: Microsoft.AspNetCore.Hosting.Diagnostics[1]
Request starting HTTP/2 GET https://localhost:7XXX/swagger/index.html
```

### Passo 5: Acesse o Swagger

Abra seu navegador e acesse:
```
https://localhost:7XXX/swagger
```

(Substitua `7XXX` pela porta exibida no console)

---

## ?? Testes

### Testar via Swagger

1. **Obter AccountId do Seed**
   - Na saída do console, copie o `Seed AccountId`

2. **Testar Operação de Crédito**
   ```json
   POST /api/credit
   {
     "accountId": "87654321-4321-4321-4321-210987654321",
     "operationId": "12345678-1234-1234-1234-111111111111",
     "amount": 100.00
   }
   ```

3. **Testar Operação de Débito**
   ```json
   POST /api/debit
   {
     "accountId": "87654321-4321-4321-4321-210987654321",
     "operationId": "12345678-1234-1234-1234-222222222222",
     "amount": 50.00
   }
   ```

### Testar Idempotência

Faça a mesma requisição 2 vezes com o mesmo `operationId`:
- **1ª requisição**: Sucesso (201/200)
- **2ª requisição**: Sucesso mas operação não é duplicada (idempotência garantida)

### Testar Concorrência

Use uma ferramenta como **Apache JMeter** ou **Postman Runners** para enviar 10 requisições simultâneas na mesma conta. O `AccountLockManager` garantirá que sejam processadas sequencialmente.

### Testar com PostgreSQL

```bash
# Conectar ao PostgreSQL
docker exec -it pagueveloz-postgres psql -U pagueveloz -d pagueveloz

# Listar todas as operações de uma conta
SELECT id, "Type", "Status", "Amount", "CreatedAt" 
FROM "Operations" 
WHERE "AccountId" = '87654321-4321-4321-4321-210987654321'
ORDER BY "CreatedAt" DESC;

# Verificar saldo da conta
SELECT "Id", "Balance", "ReservedBalance", "CreditLimit", "Status"
FROM "Accounts"
WHERE "Id" = '87654321-4321-4321-4321-210987654321';
```

### Testar RabbitMQ

1. Acesse: `http://localhost:15672`
2. Credenciais: `pagueveloz` / `StrongPassword123!`
3. Vá para **Exchanges** ? `pagueveloz.operations`
4. Você deve ver mensagens sendo publicadas após cada operação

---

## ?? API Endpoints

### Debit (Débito)
```
POST /api/debit
Content-Type: application/json

{
  "accountId": "guid",
  "operationId": "guid",
  "amount": 100.00
}
```

**Respostas:**
- `200 OK`: Débito processado com sucesso
- `400 Bad Request`: Saldo insuficiente
- `404 Not Found`: Conta não encontrada
- `409 Conflict`: Operação já existe (idempotência)

### Credit (Crédito)
```
POST /api/credit
Content-Type: application/json

{
  "accountId": "guid",
  "operationId": "guid",
  "amount": 100.00
}
```

### Reserve (Reserva de Saldo)
```
POST /api/reserve
Content-Type: application/json

{
  "accountId": "guid",
  "operationId": "guid",
  "amount": 50.00
}
```

### Capture (Capturar Reserva)
```
POST /api/capture
Content-Type: application/json

{
  "accountId": "guid",
  "operationId": "guid",
  "amount": 50.00
}
```

### Release (Liberar Reserva)
```
POST /api/release
Content-Type: application/json

{
  "accountId": "guid",
  "operationId": "guid",
  "amount": 50.00
}
```

### Transfer (Transferência)
```
POST /api/transfer
Content-Type: application/json

{
  "sourceAccountId": "guid",
  "destinationAccountId": "guid",
  "operationId": "guid",
  "amount": 100.00
}
```

### Revert (Reverter Operação)
```
POST /api/revert
Content-Type: application/json

{
  "accountId": "guid",
  "operationId": "guid"
}
```

---

## ?? Diferenciais Implementados

### ? Docker Compose
- PostgreSQL 16 Alpine com volume persistente
- RabbitMQ 3.11 Management UI
- Health checks configurados
- Dependência entre serviços

### ? Resiliência
- **Retry com Backoff Exponencial**: 200ms ? 400ms ? 800ms
- **Idempotência**: OperationId deduplicado
- **Locks Distribuídos**: AccountLockManager previne race conditions
- **Transações ACID**: PostgreSQL com EF Core

### ? Observabilidade
- Seed automático de dados de teste
- Logs estruturados no console
- Migration automática no startup
- Error handling robusto

### ? Escalabilidade
- Arquitetura preparada para divisão em microsserviços
- Camadas bem separadas (Core, Application, Infrastructure)
- DI Container facilitando testes

---

## ?? Troubleshooting

### Erro: "Connection refused" ao conectar PostgreSQL

**Causa**: PostgreSQL não está rodando

**Solução**:
```bash
docker compose up -d
docker compose logs postgres
```

### Erro: "vhost / not found" no RabbitMQ

**Causa**: Virtual host `/pagueveloz` não existe

**Solução**: Já está configurado no `docker-compose.yml`. Reinicie:
```bash
docker compose down -v
docker compose up -d
```

### Erro: "The instance of entity type 'AccountEntity' cannot be tracked"

**Causa**: EF Core rastreando múltiplas instâncias da mesma entidade

**Solução**: Já foi corrigido nos repositórios com `.AsNoTracking()` e detach manual

### Migrations não aplicadas automaticamente

**Causa**: Erro no `Program.cs` ao chamar `ApplyMigrationsAsync()`

**Solução**: Aplicar manualmente:
```bash
dotnet ef database update --project PagueVeloz.Infrastructure --startup-project PagueVeloz.Api
```

### RabbitMQ: "None of the specified endpoints were reachable"

**Causa**: RabbitMQ não está acessível no `localhost:5672`

**Solução**:
1. Verifique se está rodando: `docker ps`
2. Verifique a porta: `docker port rabbitmq`
3. Reinicie: `docker compose restart rabbitmq`

---

## ?? Estrutura de Dados

### Clientes (Clients)
```sql
Id          UUID PRIMARY KEY
Name        VARCHAR(255) NOT NULL
Email       VARCHAR(255) NOT NULL
CreatedAt   TIMESTAMP DEFAULT CURRENT_TIMESTAMP
UpdatedAt   TIMESTAMP
```

### Contas (Accounts)
```sql
Id                  UUID PRIMARY KEY
ClientId            UUID FOREIGN KEY ? Clients
Balance             NUMERIC(18,2)       -- Saldo atual
AvailableBalance    NUMERIC(18,2)       -- Balance + CreditLimit - ReservedBalance
ReservedBalance     NUMERIC(18,2)       -- Valor reservado
CreditLimit         NUMERIC(18,2)       -- Limite de crédito
Status              VARCHAR(50)         -- "Active", "Inactive"
CreatedAt           TIMESTAMP DEFAULT CURRENT_TIMESTAMP
UpdatedAt           TIMESTAMP
```

### Operações (Operations)
```sql
Id          UUID PRIMARY KEY
AccountId   UUID FOREIGN KEY ? Accounts
Type        VARCHAR(50) NOT NULL    -- "Credit", "Debit", "Reserve", "Capture", "Release", "Transfer"
Status      VARCHAR(50) NOT NULL    -- "Pending", "Completed", "Failed", "Reverted"
Amount      NUMERIC(18,2)
CreatedAt   TIMESTAMP DEFAULT CURRENT_TIMESTAMP
CompletedAt TIMESTAMP
UpdatedAt   TIMESTAMP
```

---

## ?? Enums

### EOperationType
```csharp
Credit = 1,
Debit = 2,
Reserve = 3,
Capture = 4,
Release = 5,
Transfer = 6,
Refund = 7
```

### EOperationStatus
```csharp
Pending = 1,
Completed = 2,
Failed = 3,
Reverted = 4
```

### EAccountStatus
```csharp
Active = 1,
Inactive = 2
```

---

## ?? Referências

- [.NET 9 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [RabbitMQ .NET Client](https://www.rabbitmq.com/tutorials/tutorial-one-dotnet.html)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design](https://www.domainlanguage.com/ddd/)

---

## ?? Autor

**Jailson Junior**  
GitHub: [JJuniorS](https://github.com/JJuniorS)  
Repositório: [PagueVeloz](https://github.com/JJuniorS/PagueVeloz)

---

## ?? Licença

MIT License - veja LICENSE para detalhes.

---

## ? Status do Projeto

- ? **Fase 1**: Operações Completas (Crédito, Débito, Reserva, Captura, Liberação, Transferência, Estorno)
- ? **Fase 2**: Resiliência (Retry exponencial, Idempotência, Locks)
- ? **Fase 3**: Testes (Unitários, Integração)
- ? **Fase 4**: Observabilidade (Logs, Métricas)
- ? **Fase 5**: Diferenciais (Prometheus, Grafana, Deploy)

---

**Última atualização**: 2024
