---
name: modelo-dados-controle-viagens
description: "Use ao consultar ou implementar dados do PostgreSQL do Controle de Viagens, especialmente passageiro, trechos e viagem."
---

# Modelo de dados: Controle de Viagens

## Tabelas (schema `public`, confirmado no catálogo PostgreSQL)

- `passageiros`: `id_pass numeric` PK (default `passageiro_id_pass_seq`), `nome varchar(100)` obrigatório.
- `trechos`: `id_trecho numeric` PK (default `trechos_id_trecho_seq`), `origem char(20)`, `destino char(20)`, `valor money`, todos obrigatórios.
- `viagem`: `id_viagem numeric` PK (default `viagem_id_viagem_seq`), `id_passageiro numeric` FK → `passageiros.id_pass`, `id_trecho numeric` FK → `trechos.id_trecho`, `data_viagem date`, `pago boolean` (default `false`), todos obrigatórios; `grupo_pagamento numeric` opcional (nulo = não faturada), preenchido no faturamento com `nextval('viagem_grupo_pagamento_seq')`, um mesmo número para todas as viagens faturadas juntas. As FKs usam `ON DELETE RESTRICT`: passageiros e trechos com viagens não podem ser excluídos. Estrutura aplicada por `ControleViagens.Api/Database/Viagem.sql`.
- `meta`: metas mensais do dashboard (tela Consultas). PK (`ano integer`, `mes integer` 1–12), `meta_viagens integer`, `meta_faturamento numeric(12,2)`, ambos ≥ 0 e obrigatórios. Mês sem linha = sem meta. O faturamento realizado é a soma de `trechos.valor` de todas as viagens do mês (pagas ou não), por `data_viagem`. Estrutura aplicada por `ControleViagens.Api/Database/Meta.sql`.
Antes de alterar SQL de leitura ou escrita, confira `information_schema` ou o catálogo PostgreSQL caso a estrutura possa ter mudado.

## Convenções

- Prefira comandos parametrizados no Npgsql.
- Não exponha exceções, connection strings ou detalhes de autenticação nas respostas da API.
- Mantenha as consultas no backend; o aplicativo Blazor WebAssembly não acessa o PostgreSQL diretamente.