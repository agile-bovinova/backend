# 🐮 VacApp / BoviSense — Guía del Simulador IoT y Demostración de la IA

Esta guía explica técnicamente cómo funciona el motor de Inteligencia Artificial de VacApp, qué parámetros acepta el simulador IoT y cuál es el procedimiento exacto para demostrar la reactividad de la IA en tiempo real.

---

## 1. Parámetros del Simulador IoT (`simulator.py`)

El simulador (`REPO/ESP32/simulator/simulator.py`) emula un collar ESP32 físico enviando telemetría al broker MQTT (`localhost:1883`, topic `vacapp/telemetry`).

### Comando base
```bash
python3 simulator.py --device-id COL-001 --scenario normal --interval 3
```

### Tabla de Parámetros de Línea de Comandos

| Parámetro | Tipo | Por defecto | Descripción |
| :--- | :--- | :--- | :--- |
| `--device-id` | `string` | `esp32-001` | Identificador del collar asignado en la web (ej. `COL-001`, `COL-002`, `COL-004`). El backend lo resuelve al bovino correspondiente. |
| `--scenario` | `string` | Aleatorio | Fuerza un perfil de biomarcadores específico (`normal`, `fever`, `tachycardia`, `critical`, `hypothermia`). |
| `--interval` | `float` | `5.0` | Frecuencia de emisión en segundos. **Para demos se recomienda `3`** para ver cambios rápidos. |
| `--count` | `int` | `0` | Cantidad de lecturas a enviar antes de terminar (`0` = infinito hasta presionar `Ctrl + C`). |
| `--host` | `string` | `localhost` | Dirección IP o host del broker MQTT. |
| `--port` | `int` | `1883` | Puerto del broker MQTT. |

### Escenarios Clínicos Disponibles

- **`normal`:** Temperatura 38.2 °C – 39.3 °C \| Pulso 45 – 75 BPM.
  - *Efecto:* Mantiene la IA en **🟢 Bajo (< 15%)**. Parámetros dentro de rangos biológicos.
- **`fever`:** Temperatura 40.0 °C – 41.5 °C \| Pulso 45 – 75 BPM.
  - *Efecto:* Provoca aceleración térmica brusca. Dispara la IA a **🔴 Alto (> 85%)** y genera alerta roja.
- **`tachycardia`:** Temperatura 38.2 °C – 39.3 °C \| Pulso 90 – 130 BPM.
  - *Efecto:* Pulso acelerado persistente. IA en **🟡 Moderado** por esfuerzo o estrés cardiovascular.
- **`critical`:** Temperatura 40.5 °C – 42.0 °C \| Pulso 95 – 140 BPM.
  - *Efecto:* Hipertermia extrema y taquicardia severa simultáneas. IA en **🔴 Crítico (> 95%)**.
- **`hypothermia`:** Temperatura 36.0 °C – 37.8 °C \| Pulso 45 – 75 BPM.
  - *Efecto:* Descenso térmico anormal. Alerta por exposición ambiental o colapso.

---

## 2. ¿Cómo Funciona el Modelo de IA por Dentro?

El servicio en el backend (`PredictiveHealthService.cs`) ejecuta un modelo de Machine Learning exportado en formato **ONNX** (`bovine_health_risk_model.onnx`).

### Vector de Entrada (Tensor de 6 Características)
Cada vez que se evalúa la salud de un animal, el backend extrae su historial y alimenta al modelo con:
1. **`currentTemp`:** Última temperatura corporal registrada (°C).
2. **`currentHr`:** Última frecuencia cardíaca registrada (BPM).
3. **`tempSlope3h`:** Derivada/pendiente térmica reciente (`temp_actual - temp_anterior`). Mide qué tan rápido está subiendo o bajando la fiebre.
4. **`hrSlope3h`:** Derivada/pendiente de aceleración del ritmo cardíaco.
5. **`tempVar3h`:** Desviación estándar (inestabilidad/volatilidad) de las lecturas.
6. **`thiIndex`:** Índice Temperatura-Humedad bioclimático (THI).

### Salida del Modelo
El modelo devuelve una distribución de probabilidad de riesgo:
- **🟢 Bajo (< 35%):** Parámetros óptimos. Diagnóstico: Salud estable.
- **🟡 Moderado (35% – 70%):** Oscilación o estrés temprano. Diagnóstico: Monitoreo preventivo y sombra.
- **🔴 Alto (> 70%):** Derivada térmica elevada y taquicardia. Diagnóstico: Infección febril inminente (aislamiento y antipirético).

---

## 3. Procedimiento Práctico para Demostrar la IA

Para demostrar el funcionamiento de la IA no necesitás esperar horas de reloj. El procedimiento se divide en dos pasos claros:

### Paso A: Demostración de Clasificación Clínica (Datos Precargados)
Mostrá cómo el modelo analiza tensores y clasifica con precisión diagnóstica a los 3 bovinos seedeados:

1. Abrí **Monitoreo IoT** en la web.
2. Seleccioná a **Margarita (`BOV-001`)**:
   - Parámetros: ~38.5 °C y 60 BPM.
   - Diagnóstico IA: **🟢 Bajo (1.7%)** — "Biomarcadores estables en rangos óptimos."
3. Seleccioná a **Esperanza (`BOV-003`)**:
   - Parámetros: Oscilación térmica hacia 39.5 °C.
   - Diagnóstico IA: **🟡 Moderado (61.5%)** — "Leve oscilación térmica o estrés ambiental por calor (THI)."
4. Seleccioná a **Relámpago (`BOV-002`)**:
   - Parámetros: Curva ascendente de 40.8 °C y 88 BPM.
   - Diagnóstico IA: **🔴 Alto (93.4%)** — "Aceleración térmica rápida con pulso acelerado. Riesgo febril inminente en 12-24h."

---

### Paso B: Demostración de Reactividad en Tiempo Real (En Caliente)
Aquí demostrás cómo la IA reacciona dinámicamente frente a una anomalía inyectada:

1. En la web, dejá en pantalla a **Margarita (`BOV-001`)**, que está sana en verde (1.7%).
2. En tu terminal, ejecutá el simulador forzando fiebre aguda sobre su collar:
   ```bash
   python3 simulator.py --device-id COL-001 --scenario fever --interval 3
   ```
3. **¿Qué sucede en los siguientes 6 a 9 segundos?**
   - El simulador envía lecturas a 41.2 °C cada 3 segundos.
   - La terminal muestra: `<- Respuesta backend: Anomalía biométrica detectada (LED ENCENDIDO ⚠)`.
   - La pendiente térmica (`tempSlope3h`) se dispara instantáneamente a `+2.70 °C`.
   - El gráfico de telemetría en vivo salta fuera del umbral normal.
   - La tarjeta de *Última Lectura* pasa a estado rojo *"Fuera de rango"*.
   - El **Diagnóstico Predictivo IA** salta de **🟢 1.7%** a **🔴 > 90%** en vivo, cambiando su recomendación a *"Aislamiento preventivo y preparación de antipirético"*.
   - En la sección **Alertas** aparece de inmediato una alerta crítica roja no leída.
4. **Demostrar Recuperación:**
   Detené el simulador (`Ctrl + C`) y ejecútalo en modo normal:
   ```bash
   python3 simulator.py --device-id COL-001 --scenario normal --interval 3
   ```
   Las lecturas vuelven a 38.5 °C, la pendiente se estabiliza y la IA refleja la normalización.

---

### Paso C: Demostración con Hardware Físico (ESP32)
El hardware real complementa la demo demostrando el flujo de aprovisionamiento físico:
1. En **Ganado**, creá un animal nuevo (ej. *Toro Gitano*, caravana `BOV-004`).
2. Asignale el **Collar 4 (COL-004)**.
3. Encendé el microcontrolador ESP32 físico configurado con `DEVICE_ID = "COL-004"`.
4. En **Monitoreo IoT**, mostrá cómo recibe la primera telemetría real desde el sensor físico por Wi-Fi/MQTT.
