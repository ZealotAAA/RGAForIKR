# RGASDK API 快速参考

## 连接

```csharp
using var client = new RGACLient("COM3", 115200);
client.SlaveAddress = 1;
```

---

## RF 控制

| 方法 | 参数 | 说明 |
|------|------|------|
| `SetRFFrequency(uint frequencyHz)` | 频率(Hz)，如 2000000 | 设置 RF 频率 |
| `SetRFCenterVoltage(double voltage)` | 电压(V)，范围 5~12 | 设置 RF 中心电压 |
| `DisableRFVoltage()` | - | 禁用 RF 电压 |
| `EnableRFVoltage()` | - | 使能 RF 电压 |
| `SetRFPhase(RFPhaseMode mode)` | `Positive` / `Negative` | 设置 RF 相位 |
| `SetRFCurrentRange(RFCurrentRange range)` | `mA2` / `mA5` | 设置 RF 电流档位 |

---

## 扫描控制

| 方法 | 参数 | 说明 |
|------|------|------|
| `SetScanParameters(ScanParameters)` | 片段数、起始质量、步数、驻留时间 | 设置扫描参数 |
| `StartScan(ScanConfig)` | 片段、循环次数、建立时间、间隔 | 开始扫描 |
| `StopScan()` | - | 停止扫描 |
| `ReadMassScanData(start, end, step)` | 起始、终止质量数、步长 | 读取扫描数据 |

### ScanParameters 参数

```csharp
var scanParams = new ScanParameters
{
    SegmentCount = 1,       // 1~100
    StartMassNumber = 10,  // 0~2000
    StepCount = 50,        // 0~2000
    DwellTime = 20        // 0~16000 ms
};
```

### ScanConfig 参数

```csharp
var scanConfig = new ScanConfig
{
    ScanSegment = 1,           // 扫描片段
    LoopCount = 10000,        // 循环次数 (999=无限)
    SettlingTime = 200,       // 建立时间 (us)
    SegmentInterval = 10000   // 段间隔 (us)
};
```

---

## 灯丝控制

| 方法 | 参数 | 说明 |
|------|------|------|
| `SelectFilament(FilamentSelect)` | `FilamentA` / `FilamentB` | 选择灯丝 |
| `SetFilamentEmission(ushort ua)` | 电流(uA)，范围 0~2000 | 设置发射电流 |
| `SetDischargeTime(ushort us)` | 时间(us) | 设置放电时间 |
| `SetFilamentCurrentSafety(double A)` | 电流(A) | 设置电流安全值 |

---

## EM 板控制

| 方法 | 参数 | 说明 |
|------|------|------|
| `SetDET_HVVoltage(ushort v)` | 电压(V)，范围 0~2200 | 设置 DET_HV 电压 |
| `SetAnodeVoltage(ushort v)` | 电压(V)，范围 0~3000 | 设置 Anode 电压 |
| `SetFocusVoltage(ushort v)` | 电压(V)，范围 0~320 | 设置 Focus 电压 |
| `SetElectronEnergy(ushort eV)` | 能量(eV)，范围 0~320 | 设置电子能量 |

---

## FPGA 检测器控制

| 方法 | 参数 | 说明 |
|------|------|------|
| `SetDetectorOutB(ushort value)` | 0~4096 | 设置 OutB |
| `SetOutCLower(ushort value)` | 0~4096 | 设置 OutC (Lower) |
| `SetOutDUpper(ushort value)` | 0~4096 | 设置 OutD (Upper) |

---

## 系统控制

| 方法 | 参数 | 说明 |
|------|------|------|
| `SaveParameters()` | - | 保存参数 |
| `SetBaudRate(BaudRateSetting)` | `Baud9600` / `Baud115200` / `Baud921600` | 设置波特率 |
| `ClearErrorFlag()` | - | 清除错误标记 |
| `StartCalibration()` | - | 开始校验 |
| `SetVacuumCoefficient(double)` | 真空系数 | 设置真空系数校正 |

---

## 读取操作

| 方法 | 返回类型 | 说明 |
|------|----------|------|
| `ReadPSHeartbeat()` | ushort | 读取 PS 板心跳 |
| `ReadSYSHeartbeat()` | ushort | 读取 SYS 板心跳 |
| `ReadVacuumDegree()` | double | 读取真空度 (Torr) |
| `ReadAnodeVoltage()` | double | 读取 Anode 电压 (V) |
| `ReadEMVoltage()` | double | 读取 EM 电压 (V) |
| `ReadFilamentCurrent()` | double | 读取灯丝电流 (A) |
| `ReadFilamentEmissionCurrent()` | ushort | 读取发射电流 (uA) |
| `ReadFocusVoltage()` | ushort | 读取 Focus 电压 |
| `ReadElectronEnergy()` | double | 读取电子能量 (eV) |
| `ReadRFFrequency()` | uint | 读取 RF 频率 (Hz) |
| `ReadStatus()` | RGAStatusInfo | 读取完整状态 |

---

## 设备初始化

```csharp
var emConfig = new EMBoardConfig {
    DET_HVVoltage = 200,
    AnodeVoltage = 200,
    FocusVoltage = 10,
    ElectronEnergy = 70
};

var rfConfig = new RFConfig {
    Frequency = 2000000,
    CenterVoltage = 5.0,
    PhaseMode = RFPhaseMode.Positive,
    CurrentRange = RFCurrentRange.mA2
};

var filamentConfig = new FilamentConfig {
    Filament = FilamentSelect.FilamentA,
    EmissionCurrent = 1000,
    DischargeTime = 200
};

var result = client.InitializeDevice(emConfig, rfConfig, filamentConfig);
```

---

## 结果处理

```csharp
// 设置操作
var result = client.SetRFFrequency(2000000);
if (result.Success)
    Console.WriteLine("成功");
else
    Console.WriteLine($"失败: {result.ErrorMessage}");

// 读取操作
var vacuum = client.ReadVacuumDegree();
if (vacuum.Success)
    Console.WriteLine($"真空度: {vacuum.Value}");
```

---

## 事件订阅

```csharp
client.LogReceived += (s, msg) => Console.WriteLine($"[LOG] {msg}");
client.ErrorOccurred += (s, ex) => Console.WriteLine($"[ERROR] {ex.Message}");
```

---

## 枚举速查

### RFPhaseMode
- `Positive` = 0x0001 (正极性)
- `Negative` = 0x0002 (负极性)

### RFCurrentRange
- `mA2` = 0x0001 (2mA)
- `mA5` = 0x0002 (5mA)

### FilamentSelect
- `FilamentA` = 0x0001
- `FilamentB` = 0x0002

### BaudRateSetting
- `Baud9600` = 0x01
- `Baud115200` = 0x02
- `Baud921600` = 0x03
