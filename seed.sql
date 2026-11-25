-- =============================================================================
-- VacApp / BoviSense - Script de Inicialización y Seeding de Base de Datos
-- =============================================================================
-- Este script limpia y siembra datos realistas y consistentes en PostgreSQL
-- para todos los módulos: Usuarios, Establos, Ganado, Collares, Telemetría,
-- Alertas Sanitarias, Categorías, Inventario de Productos y Campañas Sanitarias.
--
-- Puedes editar los datos del usuario en la Sección 1 (nombre, email, password).
--
-- Para ejecutar este script con Docker:
--   docker exec -i mi_postgres psql -U root -d vacapp < seed.sql
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 0. Limpieza en cascada respetando llaves foráneas
-- -----------------------------------------------------------------------------
DELETE FROM "campaign-bovines";
DELETE FROM "campaign-stables";
DELETE FROM campaigns;
DELETE FROM alerts;
DELETE FROM "bovine-health-records";
DELETE FROM collars;
DELETE FROM bovines;
DELETE FROM products;
DELETE FROM categories;
DELETE FROM stables;
DELETE FROM users;

-- -----------------------------------------------------------------------------
-- 1. Usuario Ganadero Principal (Personalizable)
-- -----------------------------------------------------------------------------
-- Contraseña por defecto: password123 (hash BCrypt estándar)
INSERT INTO users (id, username, password, email, role, "created-at", "updated-at")
VALUES (
    1,
    'Franco',
    '$2a$11$H6gDcmeiE.lZEcE2Q526Ae6DL0d4GwCUwrjzEueIyXGxbCON3rAz.',
    'franco@bovinova.com',
    'Rancher',
    NOW(),
    NOW()
);

-- -----------------------------------------------------------------------------
-- 2. Establos
-- -----------------------------------------------------------------------------
INSERT INTO stables (id, name, "limit", user_id)
VALUES 
    (1, 'Establo Norte - Lechería', 15, 1),
    (2, 'Establo Sur - Engorde y Cría', 10, 1);

-- -----------------------------------------------------------------------------
-- 3. Bovinos (3 Perfiles Clínicos con SVGs Vectoriales en Base64)
-- -----------------------------------------------------------------------------
-- Bovino 1: Margarita (Hembra, Holstein) -> Perfil Sano (Verde en IA: ~1.7%)
-- Bovino 2: Relámpago (Macho, Angus)     -> Perfil Febril Crítico (Rojo en IA: ~93.4%)
-- Bovino 3: Esperanza (Hembra, Jersey)   -> Perfil Estrés Moderado (Amarillo en IA: ~61.5%)

INSERT INTO bovines (
    id, name, code, gender, "birth-date", breed, "stable-id", "bovine-img", user_id,
    "min-temperature", "max-temperature", "min-heart-rate", "max-heart-rate"
)
VALUES 
(
    1,
    'Margarita',
    'BOV-001',
    'Hembra',
    TIMESTAMPTZ '2023-03-12 00:00:00+00',
    'Holstein',
    1,
    'data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAxMjAgMTIwIj48Y2lyY2xlIGN4PSI2MCIgY3k9IjYwIiByPSI1OCIgZmlsbD0iI0VDRkRGNSIgc3Ryb2tlPSIjQTdGM0QwIiBzdHJva2Utd2lkdGg9IjIiLz48ZWxsaXBzZSBjeD0iNjAiIGN5PSI2NSIgcng9IjM2IiByeT0iMzAiIGZpbGw9IiNGRkZGRkYiIHN0cm9rZT0iIzM3NDE1MSIgc3Ryb2tlLXdpZHRoPSIyLjUiLz48cGF0aCBkPSJNIDQwIDQyIEMgNDUgNDggNTUgNDUgNTIgMzggWiIgZmlsbD0iIzFGMjkzNyIvPjxwYXRoIGQ9Ik0gNzIgNDUgQyA4MCA1MCA4NSA0MiA4MCAzOCBaIiBmaWxsPSIjMUYyOTM3Ii8+PGVsbGlwc2UgY3g9IjYwIiBjeT0iNzQiIHJ4PSIyMiIgcnk9IjE0IiBmaWxsPSIjRkNFN0YzIiBzdHJva2U9IiMzNzQxNTEiIHN0cm9rZS13aWR0aD0iMiIvPjxjaXJjbGUgY3g9IjUzIiBjeT0iNzQiIHI9IjIuNSIgZmlsbD0iIzM3NDE1MSIvPjxjaXJjbGUgY3g9IjY3IiBjeT0iNzQiIHI9IjIuNSIgZmlsbD0iIzM3NDE1MSIvPjxjaXJjbGUgY3g9IjQ4IiBjeT0iNTgiIHI9IjMuNSIgZmlsbD0iIzFGMjkzNyIvPjxjaXJjbGUgY3g9IjcyIiBjeT0iNTgiIHI9IjMuNSIgZmlsbD0iIzFGMjkzNyIvPjxwYXRoIGQ9Ik0gMzYgNDQgQyAyNiAzOCAyMiA0OCAyOCA1NCBDIDMzIDU0IDM2IDUwIDM2IDQ0IFoiIGZpbGw9IiNGQ0U3RjMiIHN0cm9rZT0iIzM3NDE1MSIgc3Ryb2tlLXdpZHRoPSIyIi8+PHBhdGggZD0iTSA4NCA0NCBDIDk0IDM4IDk4IDQ4IDkyIDU0IEMgODcgNTQgODQgNTAgODQgNDQgWiIgZmlsbD0iI0ZDRTdGMyIgc3Ryb2tlPSIjMzc0MTUxIiBzdHJva2Utd2lkdGg9IjIiLz48cGF0aCBkPSJNIDQ0IDQwIEMgNDAgMjYgMzQgMzAgMzggMzgiIGZpbGw9Im5vbmUiIHN0cm9rZT0iI0Q5NzcwNiIgc3Ryb2tlLXdpZHRoPSIzIiBzdHJva2UtbGluZWNhcD0icm91bmQiLz48cGF0aCBkPSJNIDc2IDQwIEMgODAgMjYgODYgMzAgODIgMzgiIGZpbGw9Im5vbmUiIHN0cm9rZT0iI0Q5NzcwNiIgc3Ryb2tlLXdpZHRoPSIzIiBzdHJva2UtbGluZWNhcD0icm91bmQiLz48L3N2Zz4=',
    1,
    38.0, 39.3, 40, 80
),
(
    2,
    'Relámpago',
    'BOV-002',
    'Macho',
    TIMESTAMPTZ '2022-06-18 00:00:00+00',
    'Angus',
    2,
    'data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAxMjAgMTIwIj48Y2lyY2xlIGN4PSI2MCIgY3k9IjYwIiByPSI1OCIgZmlsbD0iI0ZGRjdFRCIgc3Ryb2tlPSIjRkVEN0FBIiBzdHJva2Utd2lkdGg9IjIiLz48ZWxsaXBzZSBjeD0iNjAiIGN5PSI2NSIgcng9IjM4IiByeT0iMzIiIGZpbGw9IiMzNzQxNTEiIHN0cm9rZT0iIzFGMjkzNyIgc3Ryb2tlLXdpZHRoPSIyLjUiLz48ZWxsaXBzZSBjeD0iNjAiIGN5PSI3NSIgcng9IjIyIiByeT0iMTQiIGZpbGw9IiM0QjU1NjMiIHN0cm9rZT0iIzFGMjkzNyIgc3Ryb2tlLXdpZHRoPSIyIi8+PGNpcmNsZSBjeD0iNTMiIGN5PSI3NSIgcj0iMi41IiBmaWxsPSIjMTExODI3Ii8+PGNpcmNsZSBjeD0iNjciIGN5PSI3NSIgcj0iMi41IiBmaWxsPSIjMTExODI3Ii8+PGNpcmNsZSBjeD0iNDciIGN5PSI1NyIgcj0iMy41IiBmaWxsPSIjRjlGQUZCIi8+PGNpcmNsZSBjeD0iNzMiIGN5PSI1NyIgcj0iMy41IiBmaWxsPSIjRjlGQUZCIi8+PGNpcmNsZSBjeD0iNDciIGN5PSI1NyIgcj0iMS44IiBmaWxsPSIjMTExODI3Ii8+PGNpcmNsZSBjeD0iNzMiIGN5PSI1NyIgcj0iMS44IiBmaWxsPSIjMTExODI3Ii8+PHBhdGggZD0iTSAzNiA0NiBDIDI0IDQwIDIyIDUwIDI4IDU2IEMgMzMgNTYgMzYgNTIgMzYgNDYgWiIgZmlsbD0iIzRCNTU2MyIgc3Ryb2tlPSIjMUYyOTM3IiBzdHJva2Utd2lkdGg9IjIiLz48cGF0aCBkPSJNIDg0IDQ2IEMgOTYgNDAgOTggNTAgOTIgNTYgQyA4NyA1NiA4NCA1MiA4NCA0NiBaIiBmaWxsPSIjNEI1NTYzIiBzdHJva2U9IiMxRjI5MzciIHN0cm9rZS13aWR0aD0iMiIvPjxwYXRoIGQ9Ik0gNDQgNDIgQyAzOCAyNCAyOCAyOCAzNCAzOCIgZmlsbD0ibm9uZSIgc3Ryb2tlPSIjOTI0MDBFIiBzdHJva2Utd2lkdGg9IjQiIHN0cm9rZS1saW5lY2FwPSJyb3VuZCIvPjxwYXRoIGQ9Ik0gNzYgNDIgQyA4MiAyNCA5MiAyOCA4NiAzOCIgZmlsbD0ibm9uZSIgc3Ryb2tlPSIjOTI0MDBFIiBzdHJva2Utd2lkdGg9IjQiIHN0cm9rZS1saW5lY2FwPSJyb3VuZCIvPjwvc3ZnPg==',
    1,
    37.5, 39.0, 35, 75
),
(
    3,
    'Esperanza',
    'BOV-003',
    'Hembra',
    TIMESTAMPTZ '2023-11-05 00:00:00+00',
    'Jersey',
    1,
    'data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAxMjAgMTIwIj48Y2lyY2xlIGN4PSI2MCIgY3k9IjYwIiByPSI1OCIgZmlsbD0iI0VGRjZGRiIgc3Ryb2tlPSIjQkZEQkZFIiBzdHJva2Utd2lkdGg9IjIiLz48ZWxsaXBzZSBjeD0iNjAiIGN5PSI2NSIgcng9IjM2IiByeT0iMzAiIGZpbGw9IiNEOTc3MDYiIHN0cm9rZT0iIzc4MzUwRiIgc3Ryb2tlLXdpZHRoPSIyLjUiLz48ZWxsaXBzZSBjeD0iNjAiIGN5PSI3NCIgcng9IjIyIiByeT0iMTQiIGZpbGw9IiNGREU2OEEiIHN0cm9rZT0iIzc4MzUwRiIgc3Ryb2tlLXdpZHRoPSIyIi8+PGNpcmNsZSBjeD0iNTMiIGN5PSI3NCIgcj0iMi41IiBmaWxsPSIjNzgzNTBGIi8+PGNpcmNsZSBjeD0iNjciIGN5PSI3NCIgcj0iMi41IiBmaWxsPSIjNzgzNTBGIi8+PGNpcmNsZSBjeD0iNDgiIGN5PSI1OCIgcj0iNCIgZmlsbD0iIzFGMjkzNyIvPjxjaXJjbGUgY3g9IjcyIiBjeT0iNTgiIHI9IjQiIGZpbGw9IiMxRjI5MzciLz48cGF0aCBkPSJNIDM2IDQ0IEMgMjYgMzggMjIgNDggMjggNTQgQyAzMyA1NCAzNiA1MCAzNiA0NCBaIiBmaWxsPSIjRkRFNjhBIiBzdHJva2U9IiM3ODM1MEYiIHN0cm9rZS13aWR0aD0iMiIvPjxwYXRoIGQ9Ik0gODQgNDQgQyA5NCAzOCA5OCA0OCA5MiA1NCBDIDg3IDU0IDg0IDUwIDg0IDQ0IFoiIGZpbGw9IiNGREU2OEEiIHN0cm9rZT0iIzc4MzUwRiIgc3Ryb2tlLXdpZHRoPSIyIi8+PHBhdGggZD0iTSA0NCA0MCBDIDQwIDI2IDM0IDMwIDM4IDM4IiBmaWxsPSJub25lIiBzdHJva2U9IiNCNDUzMDkiIHN0cm9rZS13aWR0aD0iMyIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIi8+PHBhdGggZD0iTSA3NiA0MCBDIDgwIDI2IDg2IDMwIDgyIDM4IiBmaWxsPSJub25lIiBzdHJva2U9IiNCNDUzMDkiIHN0cm9rZS13aWR0aD0iMyIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIi8+PC9zdmc+',
    1,
    38.0, 39.4, 42, 82
);

-- -----------------------------------------------------------------------------
-- 4. Collares IoT Vinculados
-- -----------------------------------------------------------------------------
INSERT INTO collars (id, device_id, bovine_id, user_id, registered_at, lifecycle_status)
VALUES 
    (1, 'COL-001', 1, 1, NOW(), 'Active'),
    (2, 'COL-002', 2, 1, NOW(), 'Active'),
    (3, 'COL-003', 3, 1, NOW(), 'Active');

-- -----------------------------------------------------------------------------
-- 5. Telemetría de 24 Horas Recientes (Generación Dinámica)
-- -----------------------------------------------------------------------------
-- Genera 25 lecturas horarias hacia atrás (desde NOW() - 24 horas hasta NOW()).
-- Garantiza que las gráficas y la IA siempre tengan datos frescos al correr el script.

-- Margarita (COL-001): Parámetros estables óptimos -> Predicción IA: 🟢 Bajo (< 5%)
INSERT INTO "bovine-health-records" (bovine_id, user_id, "device-id", temperature, "heart-rate", battery_level, "is-alert", "recorded-at")
SELECT 
    1,
    1,
    'COL-001',
    ROUND((38.4 + (sin(i * 0.4) * 0.2))::numeric, 1),
    ROUND(60.0 + (cos(i * 0.5) * 4.0)),
    98 - (i / 4),
    false,
    NOW() - (i || ' hours')::interval
FROM generate_series(24, 0, -1) AS i;

-- Relámpago (COL-002): Fiebre progresiva aguda (subida a 40.8°C y taquicardia) -> Predicción IA: 🔴 Alto (> 90%)
INSERT INTO "bovine-health-records" (bovine_id, user_id, "device-id", temperature, "heart-rate", battery_level, "is-alert", "recorded-at")
SELECT 
    2,
    1,
    'COL-002',
    CASE 
        WHEN i > 12 THEN ROUND((38.2 + ((24 - i) * 0.05))::numeric, 1)
        ELSE ROUND((38.8 + ((12 - i) * 0.17))::numeric, 1)
    END,
    CASE 
        WHEN i > 12 THEN ROUND(62.0 + (cos(i) * 3.0))
        ELSE ROUND(72.0 + ((12 - i) * 1.5))
    END,
    95 - (i / 5),
    (i <= 6),
    NOW() - (i || ' hours')::interval
FROM generate_series(24, 0, -1) AS i;

-- Esperanza (COL-003): Estrés térmico moderado / oscilante -> Predicción IA: 🟡 Moderado (40-65%)
INSERT INTO "bovine-health-records" (bovine_id, user_id, "device-id", temperature, "heart-rate", battery_level, "is-alert", "recorded-at")
SELECT 
    3,
    1,
    'COL-003',
    ROUND((38.8 + (sin(i * 0.6) * 0.7))::numeric, 1),
    ROUND(68.0 + (cos(i * 0.7) * 7.0)),
    94 - (i / 6),
    (sin(i * 0.6) > 0.5),
    NOW() - (i || ' hours')::interval
FROM generate_series(24, 0, -1) AS i;

-- -----------------------------------------------------------------------------
-- 6. Alertas Sanitarias
-- -----------------------------------------------------------------------------
INSERT INTO alerts (id, bovine_id, user_id, alert_type, urgency_level, status, message, created_at)
VALUES 
    (1, 2, 1, 'BiometricAnomaly', 'Red', 'Unread', 'ALERTA CRÍTICA: Se detectó taquicardia (88 BPM) y aceleración térmica continua (+0.60 °C en 3h). Riesgo elevado de fiebre.', NOW() - INTERVAL '15 minutes'),
    (2, 3, 1, 'BiometricAnomaly', 'Yellow', 'Unread', 'PRECAUCIÓN: Monitoreo preventivo por estrés térmico ambiental persistente.', NOW() - INTERVAL '45 minutes');

-- -----------------------------------------------------------------------------
-- 7. Categorías e Inventario de Productos Veterinarios
-- -----------------------------------------------------------------------------
INSERT INTO categories (id, name, "user-id")
VALUES 
    (1, 'Fármacos y Antipiréticos', 1),
    (2, 'Vacunas y Biológicos', 1),
    (3, 'Nutrición y Suplementos', 1),
    (4, 'Higiene y Desinfección', 1);

INSERT INTO products (id, name, "category-id", quantity, "user-id", "expiration-date", unit, "expiry-notification-sent-at")
VALUES 
    (1, 'Antipirético Bovino Flunixin 250ml', 1, 12, 1, NOW() + INTERVAL '6 months', 'Liters', NULL),
    (2, 'Antibiótico Oxitetraciclina L.A. 500ml', 1, 8, 1, NOW() + INTERVAL '8 months', 'Liters', NULL),
    (3, 'Antiparasitario Ivermectina 1% 500ml', 1, 15, 1, NOW() + INTERVAL '12 months', 'Liters', NULL),
    (4, 'Vacuna Antiaftosa Bivalente', 2, 60, 1, NOW() + INTERVAL '9 months', 'Doses', NULL),
    (5, 'Vacuna Quíntuple Clostridial', 2, 40, 1, NOW() + INTERVAL '7 months', 'Doses', NULL),
    (6, 'Bloque Mineral con Fósforo y Sal 25kg', 3, 20, 1, NOW() + INTERVAL '18 months', 'Units', NULL),
    (7, 'Suplemento Vitamínico ADE Inyectable 250ml', 3, 6, 1, NOW() + INTERVAL '14 months', 'Liters', NULL),
    (8, 'Concentrado Proteico para Lactancia', 3, 50, 1, NOW() + INTERVAL '4 months', 'Units', NULL),
    (9, 'Sellador de Pezones Yodado Post-Ordeño 20L', 4, 5, 1, NOW() + INTERVAL '10 months', 'Liters', NULL),
    (10, 'Desinfectante Amonio Cuaternario para Establos', 4, 10, 1, NOW() + INTERVAL '24 months', 'Liters', NULL);

-- -----------------------------------------------------------------------------
-- 8. Campañas Sanitarias (Activa, Histórica y Planificada)
-- -----------------------------------------------------------------------------
INSERT INTO campaigns (id, name, description, "start-date", "end-date", user_id)
VALUES 
    (1, 'Campaña Nacional de Vacunación contra Fiebre Aftosa 2026', 'Inmunización oficial y obligatoria de todo el ganado reproductor y lechero.', NOW() - INTERVAL '5 days', NOW() + INTERVAL '25 days', 1),
    (2, 'Plan de Desparasitación y Vitaminización Otoño', 'Aplicación de antiparasitarios internos y complejo vitamínico ADE para refuerzo inmunitario.', NOW() - INTERVAL '30 days', NOW() - INTERVAL '2 days', 1),
    (3, 'Control Reproductivo y Diagnóstico de Gestación', 'Monitoreo ecográfico estacional de preñez en hembras reproductoras.', NOW() + INTERVAL '10 days', NOW() + INTERVAL '40 days', 1);

-- Asociación de Campañas con Establos
INSERT INTO "campaign-stables" (campaign_id, stable_id)
VALUES 
    (1, 1),
    (1, 2),
    (2, 1),
    (3, 1);

-- Asociación de Campañas con Bovinos
INSERT INTO "campaign-bovines" (campaign_id, bovine_id)
VALUES 
    (1, 1),
    (1, 2),
    (1, 3),
    (2, 1),
    (2, 3),
    (3, 1),
    (3, 3);

-- -----------------------------------------------------------------------------
-- 9. Actualización de Secuencias de Identidad de PostgreSQL
-- -----------------------------------------------------------------------------
SELECT setval(pg_get_serial_sequence('users', 'id'), coalesce(max(id), 1)) FROM users;
SELECT setval(pg_get_serial_sequence('stables', 'id'), coalesce(max(id), 1)) FROM stables;
SELECT setval(pg_get_serial_sequence('bovines', 'id'), coalesce(max(id), 1)) FROM bovines;
SELECT setval(pg_get_serial_sequence('collars', 'id'), coalesce(max(id), 1)) FROM collars;
SELECT setval(pg_get_serial_sequence('categories', 'id'), coalesce(max(id), 1)) FROM categories;
SELECT setval(pg_get_serial_sequence('products', 'id'), coalesce(max(id), 1)) FROM products;
SELECT setval(pg_get_serial_sequence('campaigns', 'id'), coalesce(max(id), 1)) FROM campaigns;
SELECT setval(pg_get_serial_sequence('alerts', 'id'), coalesce(max(id), 1)) FROM alerts;
SELECT setval(pg_get_serial_sequence('"bovine-health-records"', 'id'), coalesce(max(id), 1)) FROM "bovine-health-records";

COMMIT;
