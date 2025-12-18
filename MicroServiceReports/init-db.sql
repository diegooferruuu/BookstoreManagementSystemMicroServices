-- Script para crear la base de datos y tabla para MicroServiceReports
CREATE DATABASE microservice_reports;

\c microservice_reports;

CREATE TABLE IF NOT EXISTS sale_event_records (
    "Id" UUID PRIMARY KEY,
    "SaleId" BIGINT NOT NULL,
    "Payload" TEXT NOT NULL,
    "Exchange" VARCHAR(200) NOT NULL,
    "RoutingKey" VARCHAR(200) NOT NULL,
    "ReceivedAt" TIMESTAMP NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_sale_event_records_sale_id ON sale_event_records("SaleId");
CREATE INDEX IF NOT EXISTS idx_sale_event_records_received_at ON sale_event_records("ReceivedAt");

-- Verificar la tabla
SELECT * FROM sale_event_records LIMIT 10;
