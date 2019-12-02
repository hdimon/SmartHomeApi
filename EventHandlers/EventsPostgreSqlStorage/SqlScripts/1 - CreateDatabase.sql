CREATE DATABASE SmartHomeApi ENCODING 'UTF8';

CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;

CREATE TABLE "Events" (
  "EventDate"    TIMESTAMPTZ       NOT NULL,
  "EventType"    TEXT              NOT NULL,
  "DeviceType"   TEXT              NOT NULL,
  "DeviceId"     TEXT              NOT NULL,
  "Parameter"    TEXT              NOT NULL,
  "OldValue"     TEXT              NULL,
  "NewValue"     TEXT              NULL
);

SELECT create_hypertable('"Events"', 'EventDate');