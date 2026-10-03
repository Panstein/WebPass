BEGIN;

CREATE SEQUENCE IF NOT EXISTS public.trechos_id_trecho_seq
    AS bigint
    START WITH 1
    INCREMENT BY 1;

DO $$
DECLARE
    max_id bigint;
    sequence_value bigint;
    sequence_called boolean;
BEGIN
    SELECT COALESCE(MAX(id_trecho), 0)::bigint
    INTO max_id
    FROM public.trechos;

    SELECT last_value, is_called
    INTO sequence_value, sequence_called
    FROM public.trechos_id_trecho_seq;

    IF sequence_called THEN
        PERFORM setval(
            'public.trechos_id_trecho_seq'::regclass,
            GREATEST(sequence_value, max_id),
            true);
    ELSE
        PERFORM setval(
            'public.trechos_id_trecho_seq'::regclass,
            GREATEST(sequence_value, max_id + 1),
            false);
    END IF;
END $$;

ALTER TABLE public.trechos
    ALTER COLUMN id_trecho SET DEFAULT nextval('public.trechos_id_trecho_seq'::regclass);

ALTER SEQUENCE public.trechos_id_trecho_seq
    OWNED BY public.trechos.id_trecho;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conrelid = 'public.trechos'::regclass
          AND contype = 'p') THEN
        ALTER TABLE public.trechos
            ADD CONSTRAINT trechos_pkey PRIMARY KEY (id_trecho);
    END IF;
END $$;

COMMIT;