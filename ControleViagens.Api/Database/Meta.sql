BEGIN;

-- Metas mensais do dashboard (tela Consultas): uma linha por ano/mês.
-- Mês sem linha = sem meta definida. O faturamento realizado é a soma do valor
-- de todas as viagens do mês (pagas ou não), pela data da viagem.
CREATE TABLE IF NOT EXISTS public.meta (
    ano              integer       NOT NULL,
    mes              integer       NOT NULL,
    meta_viagens     integer       NOT NULL,
    meta_faturamento numeric(12,2) NOT NULL,
    CONSTRAINT meta_pkey PRIMARY KEY (ano, mes),
    CONSTRAINT meta_mes_check CHECK (mes BETWEEN 1 AND 12),
    CONSTRAINT meta_viagens_check CHECK (meta_viagens >= 0),
    CONSTRAINT meta_faturamento_check CHECK (meta_faturamento >= 0)
);

CREATE INDEX IF NOT EXISTS viagem_data_viagem_idx ON public.viagem (data_viagem);

COMMIT;
