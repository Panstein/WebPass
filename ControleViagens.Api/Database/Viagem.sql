BEGIN;

-- id_viagem e data_viagem foram criados como arrays (numeric[] e date[]).
-- Converte para valores simples; a tabela estava vazia quando o script foi escrito.
DO $$
BEGIN
    IF (SELECT udt_name FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'viagem' AND column_name = 'id_viagem') = '_numeric' THEN
        ALTER TABLE public.viagem
            ALTER COLUMN id_viagem TYPE numeric USING id_viagem[1];
    END IF;

    IF (SELECT udt_name FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'viagem' AND column_name = 'data_viagem') = '_date' THEN
        ALTER TABLE public.viagem
            ALTER COLUMN data_viagem TYPE date USING data_viagem[1];
    END IF;
END $$;

CREATE SEQUENCE IF NOT EXISTS public.viagem_id_viagem_seq
    AS bigint
    START WITH 1
    INCREMENT BY 1;

DO $$
DECLARE
    max_id bigint;
    sequence_value bigint;
    sequence_called boolean;
BEGIN
    SELECT COALESCE(MAX(id_viagem), 0)::bigint
    INTO max_id
    FROM public.viagem;

    SELECT last_value, is_called
    INTO sequence_value, sequence_called
    FROM public.viagem_id_viagem_seq;

    IF sequence_called THEN
        PERFORM setval(
            'public.viagem_id_viagem_seq'::regclass,
            GREATEST(sequence_value, max_id),
            true);
    ELSE
        PERFORM setval(
            'public.viagem_id_viagem_seq'::regclass,
            GREATEST(sequence_value, max_id + 1),
            false);
    END IF;
END $$;

ALTER TABLE public.viagem
    ALTER COLUMN id_viagem SET DEFAULT nextval('public.viagem_id_viagem_seq'::regclass);

ALTER SEQUENCE public.viagem_id_viagem_seq
    OWNED BY public.viagem.id_viagem;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conrelid = 'public.viagem'::regclass
          AND contype = 'p') THEN
        ALTER TABLE public.viagem
            ADD CONSTRAINT viagem_pkey PRIMARY KEY (id_viagem);
    END IF;

    -- RESTRICT: passageiros e trechos com viagens registradas não podem ser excluídos.
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conrelid = 'public.viagem'::regclass
          AND conname = 'viagem_id_passageiro_fkey') THEN
        ALTER TABLE public.viagem
            ADD CONSTRAINT viagem_id_passageiro_fkey FOREIGN KEY (id_passageiro)
            REFERENCES public.passageiros (id_pass)
            ON UPDATE RESTRICT ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conrelid = 'public.viagem'::regclass
          AND conname = 'viagem_id_trecho_fkey') THEN
        ALTER TABLE public.viagem
            ADD CONSTRAINT viagem_id_trecho_fkey FOREIGN KEY (id_trecho)
            REFERENCES public.trechos (id_trecho)
            ON UPDATE RESTRICT ON DELETE RESTRICT;
    END IF;
END $$;

-- Viagens já cadastradas ficam como não pagas.
ALTER TABLE public.viagem
    ADD COLUMN IF NOT EXISTS pago boolean NOT NULL DEFAULT false;

-- Preenchido no faturamento com nextval('public.viagem_grupo_pagamento_seq');
-- todas as viagens faturadas juntas recebem o mesmo número. Nulo = não faturada.
CREATE SEQUENCE IF NOT EXISTS public.viagem_grupo_pagamento_seq
    AS bigint
    START WITH 1
    INCREMENT BY 1;

ALTER TABLE public.viagem
    ADD COLUMN IF NOT EXISTS grupo_pagamento numeric;

CREATE INDEX IF NOT EXISTS viagem_grupo_pagamento_idx ON public.viagem (grupo_pagamento);

CREATE INDEX IF NOT EXISTS viagem_id_passageiro_idx ON public.viagem (id_passageiro);
CREATE INDEX IF NOT EXISTS viagem_id_trecho_idx ON public.viagem (id_trecho);

COMMIT;
