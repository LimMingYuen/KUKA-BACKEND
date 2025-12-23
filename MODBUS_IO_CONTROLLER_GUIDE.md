# Modbus IO Controller Guide

This document provides comprehensive guidance on the Modbus IO Controller implementation in the KUKA AMR system, including ModRSim simulator setup and real device integration.

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Modbus Protocol Basics](#modbus-protocol-basics)
4. [Unit ID Explained](#unit-id-explained)
5. [ADAM-6052 Register Mapping](#adam-6052-register-mapping)
6. [Digital Inputs (DI)](#digital-inputs-di)
7. [Digital Outputs (DO)](#digital-outputs-do)
8. [Fail-Safe Value (FSV)](#fail-safe-value-fsv)
9. [Watchdog Timer (WDT)](#watchdog-timer-wdt)
10. [ModRSim Simulator Setup](#modrsim-simulator-setup)
11. [Real Device vs Simulator](#real-device-vs-simulator)
12. [Troubleshooting](#troubleshooting)

---

## Overview

The IO Controller module enables communication with industrial Modbus TCP devices (like Advantech ADAM-6052) for:

- Reading digital input states (sensors, switches)
- Controlling digital outputs (relays, indicators)
- Configuring fail-safe values for safety
- Real-time monitoring via SignalR

---

## Architecture

### System Data Flow

```
┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐
│    Frontend     │      │     Backend     │      │  Modbus Device  │
│   (Angular)     │◄────►│    (.NET)       │◄────►│  (ADAM-6052)    │
│                 │ HTTP │                 │ TCP  │  or ModRSim     │
│                 │SignalR│                │ 502  │                 │
└─────────────────┘      └─────────────────┘      └─────────────────┘
                                │
                                ▼
                         ┌─────────────┐
                         │  SQL Server │
                         │  (state/logs)│
                         └─────────────┘
```

### Backend Components

| Component | Purpose |
|-----------|---------|
| `ModbusTcpService` | Low-level Modbus TCP communication |
| `IoControllerDeviceService` | Device CRUD and status |
| `IoChannelService` | Channel state management |
| `IoPollingHostedService` | Background polling (1s interval) |
| `IoControllerHub` | SignalR real-time updates |
| `IoStateLogService` | Audit trail logging |

### Polling Flow

```
Every 1 second:
┌──────────────────────────────────────────────────────────────┐
│ IoPollingHostedService                                       │
│                                                              │
│   1. Check if SignalR clients are subscribed                 │
│   2. For each active device:                                 │
│      a. Connect to device IP:Port                            │
│      b. Read DI registers (coils 0-7)                        │
│      c. Read DO registers (coils 16-23)                      │
│      d. Compare with previous state                          │
│      e. Log changes to database                              │
│      f. Broadcast changes via SignalR                        │
│   3. Update device.LastPollUtc                               │
└──────────────────────────────────────────────────────────────┘
```

---

## Modbus Protocol Basics

### What is Modbus?

Modbus is an industrial communication protocol developed in 1979. It's the most widely used protocol for connecting industrial electronic devices.

### Modbus Variants

| Variant | Transport | Description |
|---------|-----------|-------------|
| Modbus RTU | Serial (RS-485) | Binary format, CRC checksum |
| Modbus ASCII | Serial (RS-232/485) | ASCII format, LRC checksum |
| Modbus TCP | Ethernet | TCP/IP encapsulation, port 502 |

This system uses **Modbus TCP**.

### Modbus Data Types

| Type | Access | Description |
|------|--------|-------------|
| Coils | Read/Write | Single bit (digital output) |
| Discrete Inputs | Read Only | Single bit (digital input) |
| Holding Registers | Read/Write | 16-bit word |
| Input Registers | Read Only | 16-bit word |

### Modbus Function Codes

| Code | Function | Description |
|------|----------|-------------|
| 01 | Read Coils | Read digital outputs |
| 02 | Read Discrete Inputs | Read digital inputs |
| 03 | Read Holding Registers | Read configuration/data |
| 05 | Write Single Coil | Set single digital output |
| 06 | Write Single Register | Set single register |
| 15 | Write Multiple Coils | Set multiple outputs |
| 16 | Write Multiple Registers | Set multiple registers |

---

## Unit ID Explained

### What is Unit ID?

Unit ID (also called Slave ID or Station Address) identifies which device should respond to a Modbus request.

### Modbus RTU (Serial RS-485)

Multiple devices share one serial bus. Unit ID distinguishes them:

```
                    ┌─── Device 1 (Unit ID: 1)
                    │
[Master] ──RS-485───┼─── Device 2 (Unit ID: 2)
                    │
                    └─── Device 3 (Unit ID: 3)
```

- Range: 1-247 (0 = broadcast, 248-255 reserved)
- Each device must have a unique Unit ID

### Modbus TCP (Direct Connection)

Each device has its own IP address:

```
[Master] ──TCP/IP── Device (Unit ID: usually 1)
```

- Unit ID is often ignored or set to `1`
- Some devices still validate it for compatibility

### Modbus TCP Gateway

Gateway bridges TCP to serial devices:

```
                                        ┌─── PLC (Unit ID: 1)
                                        │
[Master] ──TCP/IP── [Gateway] ──RS-485──┼─── Sensor (Unit ID: 2)
                                        │
                                        └─── IO Module (Unit ID: 3)
```

- Gateway has one IP address
- Unit ID routes to the correct downstream device

### Configuration Recommendations

| Scenario | Unit ID Setting |
|----------|-----------------|
| Direct TCP to ADAM-6052 | `1` (default) |
| Via Modbus gateway | Match device's configured address |
| ModRSim simulator | `1` (default) |

---

## ADAM-6052 Register Mapping

The Advantech ADAM-6052 is an 8-channel isolated digital I/O module.

### Specifications

| Feature | Value |
|---------|-------|
| Digital Inputs | 8 channels (DI 0-7) |
| Digital Outputs | 8 channels (DO 0-7) |
| Protocol | Modbus TCP |
| Default Port | 502 |
| Default Unit ID | 1 |

### Coil Address Mapping

| Channel | Type | Modbus Address (1-based) | NModbus Address (0-based) |
|---------|------|--------------------------|---------------------------|
| DI 0 | Digital Input | 1 | 0 |
| DI 1 | Digital Input | 2 | 1 |
| DI 2 | Digital Input | 3 | 2 |
| DI 3 | Digital Input | 4 | 3 |
| DI 4 | Digital Input | 5 | 4 |
| DI 5 | Digital Input | 6 | 5 |
| DI 6 | Digital Input | 7 | 6 |
| DI 7 | Digital Input | 8 | 7 |
| DO 0 | Digital Output | 17 | 16 |
| DO 1 | Digital Output | 18 | 17 |
| DO 2 | Digital Output | 19 | 18 |
| DO 3 | Digital Output | 20 | 19 |
| DO 4 | Digital Output | 21 | 20 |
| DO 5 | Digital Output | 22 | 21 |
| DO 6 | Digital Output | 23 | 22 |
| DO 7 | Digital Output | 24 | 23 |

### Special Registers (Holding Registers)

| Register | Purpose |
|----------|---------|
| 40501 | Watchdog Timer Enable |
| 40502 | Watchdog Timeout (seconds) |
| 40101 | FSV Enable (8-bit bitmap) |
| 40102 | FSV Values (8-bit bitmap) |

---

## Digital Inputs (DI)

### What are Digital Inputs?

Digital inputs read ON/OFF states from external sensors or switches.

```
┌─────────────┐         ┌─────────────┐
│   Sensor    │────────►│  ADAM-6052  │────────► Your App
│  (ON/OFF)   │         │   DI 0-7    │          reads state
└─────────────┘         └─────────────┘
```

### Common Use Cases

| Input Device | Purpose |
|--------------|---------|
| Proximity sensor | Detect object presence |
| Limit switch | Detect position |
| Push button | Manual trigger |
| Door sensor | Open/closed status |
| Emergency stop | Safety circuit |

### Reading DI in Code

```csharp
// Backend: ModbusTcpService.cs
public async Task<IoReadResult> ReadDigitalInputsAsync(
    string ipAddress, int port, byte unitId, CancellationToken ct)
{
    // Reads coils at addresses 0-7 (DI 0-7)
    bool[] values = master.ReadCoils(unitId, 0, 8);
    return new IoReadResult { Success = true, Values = values };
}
```

### DI Characteristics

- **Read-only**: Cannot be changed by software
- **Real-time**: Reflects physical sensor state
- **Polling**: Backend polls every 1 second

---

## Digital Outputs (DO)

### What are Digital Outputs?

Digital outputs control external devices by switching ON/OFF.

```
┌─────────────┐         ┌─────────────┐         ┌─────────────┐
│   Your App  │────────►│  ADAM-6052  │────────►│   Relay/    │
│  (command)  │         │   DO 0-7    │         │   Device    │
└─────────────┘         └─────────────┘         └─────────────┘
```

### Common Use Cases

| Output Device | Purpose |
|---------------|---------|
| Signal light | Status indicator |
| Relay | Switch high-power device |
| Solenoid valve | Control air/fluid flow |
| Motor starter | Start/stop motor |
| Alarm buzzer | Audible alert |

### Writing DO in Code

```csharp
// Backend: ModbusTcpService.cs
public async Task<IoWriteResult> WriteDigitalOutputAsync(
    string ipAddress, int port, byte unitId,
    int channelNumber, bool value, CancellationToken ct)
{
    // Write to coil at address (16 + channelNumber)
    ushort address = (ushort)(16 + channelNumber);
    master.WriteSingleCoil(unitId, address, value);
    return new IoWriteResult { Success = true };
}
```

### DO Characteristics

- **Read/Write**: Can be controlled by software
- **Persistent**: Maintains state until changed
- **Immediate**: Changes apply instantly

---

## Fail-Safe Value (FSV)

### What is FSV?

FSV defines the **safe state** for outputs when communication is lost.

```
Normal Operation:              Communication Lost:
┌──────┐    ┌──────┐          ┌──────┐    ┌──────┐
│ App  │───►│ DO 0 │          │ App  │ ✕  │ DO 0 │
│      │    │  ON  │          │      │    │ FSV  │ ← Uses FSV value
└──────┘    └──────┘          └──────┘    └──────┘
```

### Why FSV is Important

Without FSV, outputs may remain in an unsafe state:

| Scenario | Without FSV | With FSV |
|----------|-------------|----------|
| Network failure | Output frozen (unknown state) | Output goes to safe state |
| Server crash | Motor keeps running | Motor stops (FSV=OFF) |
| Cable unplugged | Emergency light stays off | Light turns on (FSV=ON) |

### FSV Configuration

Each DO channel has two FSV settings:

| Setting | Description |
|---------|-------------|
| FSV Enabled | Whether to use FSV on communication loss |
| FSV Value | The value to set (ON or OFF) |

### FSV Recommendations by Device Type

| Device | FSV Enabled | FSV Value | Reason |
|--------|-------------|-----------|--------|
| Emergency light | Yes | ON | Must turn on for safety |
| Alarm buzzer | Yes | ON | Alert on failure |
| Conveyor motor | Yes | OFF | Stop to prevent damage |
| Cooling fan | Yes | ON | Prevent overheating |
| Door lock | Depends | Depends | Based on security policy |

### FSV in Code

```csharp
// Backend: IoChannelService.cs
public async Task<IoWriteResult> SetFsvAsync(
    int deviceId, int channelNumber, bool enabled, bool value)
{
    // Update database
    channel.FsvEnabled = enabled;
    channel.FailSafeValue = value;

    // Write to device FSV registers
    await _modbusTcpService.WriteFsvSettingAsync(...);
}
```

---

## Watchdog Timer (WDT)

### What is WDT?

The Watchdog Timer monitors communication health. If no valid communication is received within the timeout period, FSV values are applied.

```
┌────────────────────────────────────────────────────────────┐
│                    ADAM-6052 Device                        │
│                                                            │
│   ┌──────────┐     ┌──────────┐     ┌──────────┐          │
│   │ Receive  │────►│   WDT    │────►│   FSV    │          │
│   │ Command  │     │  Timer   │     │  Logic   │          │
│   └──────────┘     └──────────┘     └──────────┘          │
│        ▲              │ timeout         │                  │
│        │              ▼                 ▼                  │
│   Valid Modbus    Counter=0        Apply FSV              │
│   communication   (reset)          to outputs             │
└────────────────────────────────────────────────────────────┘
```

### How WDT Works

1. **Normal operation**: Each valid Modbus command resets the WDT counter
2. **Communication loss**: No commands received, counter increments
3. **Timeout**: Counter reaches threshold, FSV values applied
4. **Recovery**: Communication resumes, normal operation restored

### WDT Configuration

| Setting | Description | Typical Value |
|---------|-------------|---------------|
| WDT Enable | Enable/disable watchdog | Enabled |
| WDT Timeout | Seconds before FSV activates | 5-30 seconds |

### WDT + FSV Example Timeline

```
Time    Event                           DO State
────    ─────                           ────────
0s      Normal polling                  ON (app controlled)
1s      Normal polling                  ON
2s      Normal polling                  ON
3s      Network cable unplugged         ON (last value)
4s      No communication (WDT: 1s)      ON
5s      No communication (WDT: 2s)      ON
6s      No communication (WDT: 3s)      ON
7s      No communication (WDT: 4s)      ON
8s      WDT TIMEOUT (5s reached)        OFF ← FSV applied!
...
15s     Network restored                OFF (FSV active)
16s     First valid command             ON (app takes control)
```

---

## ModRSim Simulator Setup

### What is ModRSim?

ModRSim is a free Modbus slave simulator for Windows. It allows testing Modbus communication without physical hardware.

### Download

- ModRSim2: https://sourceforge.net/projects/modrssim2/

### Configuration Steps

1. **Launch ModRSim**

2. **Set Protocol**:
   - Click `File` → `New`
   - Select `Modbus TCP/IP`

3. **Configure Port**:
   - Default: `502`
   - Or use custom port (e.g., `504`)

4. **Set Unit ID**:
   - Default: `1`
   - Match your app configuration

5. **View/Edit Registers**:
   - Coils tab: Shows DI (0-7) and DO (16-23)
   - Click values to toggle ON/OFF

### ModRSim Register View

```
┌─────────────────────────────────────────────┐
│ Address │ Value │ Description               │
├─────────┼───────┼───────────────────────────┤
│ 00001   │ 0     │ DI 0 (Digital Input 0)    │
│ 00002   │ 0     │ DI 1                      │
│ ...     │       │                           │
│ 00008   │ 0     │ DI 7                      │
│ 00017   │ 0     │ DO 0 (Digital Output 0)   │
│ 00018   │ 0     │ DO 1                      │
│ ...     │       │                           │
│ 00024   │ 0     │ DO 7                      │
└─────────┴───────┴───────────────────────────┘
```

### Testing with ModRSim

#### Test 1: Read Digital Inputs

1. In ModRSim, click on coil address `00001` to set DI 0 = ON
2. Your app should show DI 0 as ON within 1-2 seconds

#### Test 2: Write Digital Output

1. In your app, toggle DO 0 to ON
2. In ModRSim, coil address `00017` should show value `1`

#### Test 3: Connection Test

1. In your app, click "Test Connection" button
2. Should show "Connected" with response time (e.g., "23ms")

### ModRSim Limitations

| Feature | Support |
|---------|---------|
| Read coils | ✅ Yes |
| Write coils | ✅ Yes |
| Read registers | ✅ Yes |
| Write registers | ✅ Yes |
| FSV logic | ❌ No |
| Watchdog Timer | ❌ No |
| Multiple connections | ⚠️ Limited |

---

## Real Device vs Simulator

### Comparison Table

| Aspect | Real ADAM-6052 | ModRSim Simulator |
|--------|----------------|-------------------|
| **Concurrent connections** | 4-16 clients | 1-2 clients |
| **Connection stability** | Industrial grade | May drop connections |
| **Rapid reconnects** | Handles well | Can cause errors |
| **FSV support** | ✅ Full support | ❌ Not supported |
| **Watchdog Timer** | ✅ Full support | ❌ Not supported |
| **Performance** | High frequency OK | May struggle |
| **Cost** | ~$100-200 USD | Free |
| **Use case** | Production | Development/testing |

### When to Use Each

| Scenario | Recommendation |
|----------|----------------|
| Initial development | ModRSim |
| API testing | ModRSim |
| UI development | ModRSim |
| FSV/WDT testing | Real device |
| Integration testing | Real device |
| Production | Real device |

### Connection Error Expectations

**With ModRSim** (expected):
```
WARN - Failed to poll device: Connection was aborted
```

**With Real Device** (should not see):
```
These errors indicate network or hardware issues
```

---

## Troubleshooting

### Common Issues

#### 1. "Connection Refused" Error

**Cause**: ModRSim not running or wrong port

**Solution**:
- Verify ModRSim is running
- Check port matches (default: 502)
- Check firewall settings

#### 2. "Connection Aborted" Error

**Cause**: ModRSim dropped the connection

**Solution**:
- This is normal with ModRSim
- Reduce polling frequency if frequent
- Will not occur with real hardware

#### 3. Device Shows "Disconnected" but Should Be Connected

**Cause**: Data sync issue between UI components

**Solution**:
- Refresh the page
- Check backend logs for actual connection status
- Verify SignalR is connected

#### 4. DI Values Not Updating

**Cause**: Polling not running or SignalR disconnected

**Solution**:
- Check backend console for polling logs
- Verify SignalR connection in browser console
- Ensure "Auto Refresh" is enabled

#### 5. Cannot Write to DO

**Cause**: Wrong register address or ModRSim issue

**Solution**:
- Verify DO addresses (17-24 for DO 0-7)
- Check ModRSim is not in read-only mode
- Check backend logs for write errors

### Debug Checklist

```
□ ModRSim running?
□ Correct IP address? (127.0.0.1 for local)
□ Correct port? (502 or 504)
□ Correct Unit ID? (usually 1)
□ Backend API running?
□ Frontend connected to backend?
□ SignalR connected?
□ Auto Refresh enabled?
□ Device marked as "Active"?
```

### Useful Log Messages

**Successful polling**:
```
INFO - Polled device Test1: 8 DI, 8 DO channels updated
```

**Connection failure**:
```
WARN - Failed to poll device Test1: Connection refused
```

**State change detected**:
```
INFO - Channel state changed: Test1/DO0 OFF→ON (Source: User)
```

---

## API Reference

### Device Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/IoController/devices` | List all devices |
| GET | `/api/IoController/devices/{id}` | Get device details |
| POST | `/api/IoController/devices` | Create device |
| PUT | `/api/IoController/devices/{id}` | Update device |
| DELETE | `/api/IoController/devices/{id}` | Delete device |
| POST | `/api/IoController/devices/{id}/test` | Test connection |
| GET | `/api/IoController/devices/{id}/status` | Get full status |

### Channel Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/IoController/devices/{id}/channels` | List channels |
| PUT | `/api/IoController/.../channels/{type}/{num}/label` | Update label |
| POST | `/api/IoController/devices/{id}/do/{num}` | Set DO value |
| POST | `/api/IoController/devices/{id}/do/{num}/fsv` | Set FSV |

### SignalR Hub

**Hub URL**: `/hubs/iocontroller`

**Client Methods**:
- `SubscribeToDevice(deviceId)` - Subscribe to device updates
- `UnsubscribeFromDevice(deviceId)` - Unsubscribe
- `SubscribeToAll()` - Subscribe to all devices
- `UnsubscribeFromAll()` - Unsubscribe from all

**Server Events**:
- `ReceiveDeviceStatus` - Full device status update
- `ReceiveChannelChange` - Single channel state change
- `ReceiveConnectionStatus` - Connection status change

---

## Glossary

| Term | Definition |
|------|------------|
| **Coil** | Single-bit Modbus data type (digital I/O) |
| **DI** | Digital Input - reads external sensor state |
| **DO** | Digital Output - controls external device |
| **FSV** | Fail-Safe Value - safe state on communication loss |
| **Holding Register** | 16-bit Modbus data type for configuration |
| **Master** | Device initiating Modbus requests (your app) |
| **Modbus** | Industrial communication protocol |
| **NModbus** | .NET library for Modbus communication |
| **Polling** | Periodically reading device status |
| **Slave** | Device responding to Modbus requests (ADAM-6052) |
| **Unit ID** | Address identifying a Modbus slave device |
| **WDT** | Watchdog Timer - monitors communication health |

---

## References

- [Modbus Protocol Specification](https://modbus.org/specs.php)
- [NModbus Library](https://github.com/NModbus/NModbus)
- [ADAM-6052 Manual](https://www.advantech.com/products/adam-6052)
- [ModRSim2 Download](https://sourceforge.net/projects/modrssim2/)
