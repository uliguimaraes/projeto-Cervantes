-- =====================================================================
-- Gerenciador de Veículos — script de criação do banco (PostgreSQL 14+)
--
-- Passo a passo:
--   1) Crie o banco:   CREATE DATABASE veiculos;
--   2) Execute este script conectado a ele:
--        psql -U postgres -d veiculos -f database/01-criacao.sql
--
-- Pode ser executado mais de uma vez sem erro. Se você já criou o banco com
-- uma versão anterior deste script, apague-o e crie de novo (a tabela Veiculo
-- ganhou as colunas Tipo, NumeroPortas e Cilindradas).
--
-- TODAS as regras de negócio do enunciado são garantidas aqui, no banco:
--   Marca   : nome obrigatório e único; não excluir marca com veículos
--   Veiculo : placa obrigatória e única; modelo obrigatório;
--             ano entre 1950 e o ano atual; veículo deve possuir marca
--   Log     : todo INSERT/UPDATE/DELETE em Veiculo vai para LogTransacao
-- =====================================================================

-- ---------------------------------------------------------------------
-- MARCA
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "Marca" (
    "Codigo" INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Nome"   VARCHAR(120) NOT NULL,
    CONSTRAINT "CK_Marca_Nome_NaoVazio" CHECK (length(btrim("Nome")) > 0)
);

-- Nome único ignorando maiúsculas/minúsculas e espaços nas pontas ("Fiat" = " FIAT ").
CREATE UNIQUE INDEX IF NOT EXISTS "UQ_Marca_Nome" ON "Marca" (lower(btrim("Nome")));

CREATE OR REPLACE FUNCTION normalizar_marca() RETURNS TRIGGER AS $$
BEGIN
    NEW."Nome" := btrim(NEW."Nome");
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "TR_Marca_Normalizar" ON "Marca";
CREATE TRIGGER "TR_Marca_Normalizar"
BEFORE INSERT OR UPDATE ON "Marca"
FOR EACH ROW EXECUTE FUNCTION normalizar_marca();

-- ---------------------------------------------------------------------
-- VEICULO
-- Herança em tabela única: "Tipo" identifica a subclasse (Carro/Motocicleta)
-- e cada tipo usa só a sua coluna específica (NumeroPortas ou Cilindradas).
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "Veiculo" (
    "Codigo"       INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Placa"        VARCHAR(10)  NOT NULL,
    "Modelo"       VARCHAR(120) NOT NULL,
    "Ano"          INTEGER      NOT NULL,
    "MarcaCodigo"  INTEGER      NOT NULL REFERENCES "Marca"("Codigo") ON DELETE RESTRICT,
    "Tipo"         VARCHAR(20)  NOT NULL,
    "NumeroPortas" INTEGER,
    "Cilindradas"  INTEGER,
    CONSTRAINT "UQ_Veiculo_Placa"        UNIQUE ("Placa"),
    CONSTRAINT "CK_Veiculo_Placa_NaoVazia" CHECK (length(btrim("Placa")) > 0),
    CONSTRAINT "CK_Veiculo_Modelo_NaoVazio" CHECK (length(btrim("Modelo")) > 0),
    CONSTRAINT "CK_Veiculo_Ano_Minimo"   CHECK ("Ano" >= 1950),
    CONSTRAINT "CK_Veiculo_Tipo"         CHECK ("Tipo" IN ('CARRO', 'MOTOCICLETA')),
    CONSTRAINT "CK_Veiculo_Detalhe"      CHECK (
        ("Tipo" = 'CARRO'
            AND "NumeroPortas" IS NOT NULL AND "NumeroPortas" BETWEEN 1 AND 6
            AND "Cilindradas" IS NULL)
        OR
        ("Tipo" = 'MOTOCICLETA'
            AND "Cilindradas" IS NOT NULL AND "Cilindradas" > 0
            AND "NumeroPortas" IS NULL)
    )
);

CREATE INDEX IF NOT EXISTS "IX_Veiculo_MarcaCodigo" ON "Veiculo" ("MarcaCodigo");

-- Normaliza a placa (maiúscula, sem espaços nas pontas) e valida o ano.
-- O limite superior ("ano atual") muda com o tempo, por isso não dá para ser um CHECK fixo.
CREATE OR REPLACE FUNCTION validar_veiculo() RETURNS TRIGGER AS $$
DECLARE
    ano_atual INTEGER := EXTRACT(YEAR FROM CURRENT_DATE)::INTEGER;
BEGIN
    NEW."Placa"  := upper(btrim(NEW."Placa"));
    NEW."Modelo" := btrim(NEW."Modelo");

    IF NEW."Ano" IS NOT NULL AND (NEW."Ano" < 1950 OR NEW."Ano" > ano_atual) THEN
        RAISE EXCEPTION 'O ano do veículo deve estar entre 1950 e %.', ano_atual;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "TR_Veiculo_Validar" ON "Veiculo";
CREATE TRIGGER "TR_Veiculo_Validar"
BEFORE INSERT OR UPDATE ON "Veiculo"
FOR EACH ROW EXECUTE FUNCTION validar_veiculo();

-- ---------------------------------------------------------------------
-- LOGTRANSACAO — operações realizadas na tabela Veiculo
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "LogTransacao" (
    "Codigo"        BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "VeiculoCodigo" INTEGER,                                  -- sem FK: o veículo pode ter sido excluído
    "TipoOperacao"  VARCHAR(10) NOT NULL,                     -- INSERT | UPDATE | DELETE
    "DataHora"      TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "CK_LogTransacao_Tipo" CHECK ("TipoOperacao" IN ('INSERT', 'UPDATE', 'DELETE'))
);

CREATE OR REPLACE FUNCTION registrar_log_veiculo() RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO "LogTransacao" ("VeiculoCodigo", "TipoOperacao")
    VALUES (COALESCE(NEW."Codigo", OLD."Codigo"), TG_OP);
    RETURN COALESCE(NEW, OLD);
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "TR_Veiculo_Log" ON "Veiculo";
CREATE TRIGGER "TR_Veiculo_Log"
AFTER INSERT OR UPDATE OR DELETE ON "Veiculo"
FOR EACH ROW EXECUTE FUNCTION registrar_log_veiculo();
