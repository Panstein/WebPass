---
name: conexao-postgresql-controle-viagens
description: "Use ao configurar, testar ou implementar acesso ao PostgreSQL do projeto Controle de Viagens."
---

# Conexão PostgreSQL: Controle de Viagens

## Parâmetros conhecidos

- Provedor: Neon (pooler `ep-icy-haze-b651hh6h-pooler.c-2.sa-east-1.aws.neon.tech`)
- Porta: `5432`
- Banco: `WebPass` (respeitar a capitalização exata do identificador PostgreSQL).
- SSL Mode: `Require`; Channel Binding: `Require`
- Produção (Render): variável de ambiente `ConnectionStrings__WebPass`; aceita a URI `postgresql://...` do Neon, convertida em `Program.cs`.
- Driver .NET: Npgsql, exclusivamente na API ASP.NET Core.
- Arquivo local: `ControleViagens.Api/appsettings.Development.json` (`ConnectionStrings:WebPass`).

Usuário e senha são mantidos somente no arquivo JSON de desenvolvimento, excluído do Git. Não copie esses segredos para esta skill, para o cliente Blazor, nem para logs ou respostas HTTP.

## Verificação

- Use `GET /api/health`, que abre conexão e executa `SELECT 1`.
- Respostas de erro HTTP não devem expor exceções nem credenciais; detalhes técnicos ficam apenas no log da API.
- Se PostgreSQL responder `no pg_hba.conf entry ... no encryption`, o servidor foi alcançado, mas não permite esse IP de origem com conexões sem TLS. Não altere o modo SSL sem autorização.