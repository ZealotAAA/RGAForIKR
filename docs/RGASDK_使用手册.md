# RGASDK 使用手册

**版本**: 1.0.0
**日期**: 2026-05-26
**协议**: Modbus RTU

---

## 一、概述

RGASDK 是用于控制 RGA（残余气体分析仪）设备的 C# 开发包，基于 Modbus RTU 通信协议开发。

### 主要特性

- 完整的 Modbus RTU 协议实现
- 支持 RF 控制、扫描控制、灯丝控制、检测器控制
- 统一的操作结果返回机制
- 事件驱动的日志和错误处理
- 线程安全的并发访问

### 系统要求

| 项目 | 要求 |
|------|------|
| .NET 版本 | .NET 8.0 及以上 |
| 操作系统 | Windows/Linux/macOS |
| 串口 | RS-232 或 USB 转串口 |

---

## 二、快速开始

### 2.1 安装

#### 方式一：项目引用
```xml
<ProjectReference Include="path\to\RGASDK.csproj" />
```

#### 方式二：NuGet 包
```bash
dotnet add package RGASDK
```

### 2.2 基本使用

```csharp
using RGASDK;

// 创建客户端并连接
using var client = new RGACLient("COM3", 115200);

// 订阅日志事件
client.LogReceived += (s, msg) => Console.WriteLine($"[LOG] {msg}");
client.ErrorOccurred += (s, ex) => Console.WriteLine($"[ERROR] {ex.Message}");

// 设置从机地址
client.SlaveAddress = 1;

// 读取真空度
var vacuum = client.ReadVacuumDegree();
if (vacuum.Success)
    Console.WriteLine($"真空度: {vacuum.Value:E2} Torr");
```

---

## 三、连接管理

### 3.1 构造函数

| 构造函数 | 说明 |
|----------|------|
| `RGACLient(string portName, int baudRate)` | 直接创建并连接 |
| `RGACLient(RGACommunicator communicator)` | 使用自定义通信器 |

### 3.2 连接方法

```csharp
// 连接设备
client.Connect("COM3", 115200);

// 断开连接
client.Disconnect();

// 检查连接状态
bool isConnected = client.IsConnected;

// 设置从机地址 (1-255)
client.SlaveAddress = 1;
```

### 3.3 串口参数

| 参数 | 值 | 说明 |
|------|-----|------|
| 波特率 | 115200 (默认) | 支持 9600/115200/921600 |
| 数据位 | 8 | - |
| 校验位 | 无 | - |
| 停止位 | 1 | - |

---

## 四、RF 控制

### 4.1 设置 RF 频率

```csharp
// frequencyHz: 频率值 (Hz)
// 典型值: 2000000 (2 MHz)
var result = client.SetRFFrequency(2000000);
```

| 参数 | 类型 | 范围 | 说明 |
|------|------|------|------|
| frequencyHz | uint | 0 ~ 4294967295 | 频率，单位 Hz |

### 4.2 设置 RF 中心电压

```csharp
// voltage: 电压值 (V)
// 范围: 5 ~ 12 V
var result = client.SetRFCenterVoltage(10.0);
```

| 参数 | 类型 | 范围 | 说明 |
|------|------|------|------|
| voltage | double | 5.0 ~ 12.0 | 电压，单位 V |

### 4.3 RF 电压使能控制

```csharp
// 禁用 RF 电压
var result = client.DisableRFVoltage();

// 使能 RF 电压
var result = client.EnableRFVoltage();
```

### 4.4 设置 RF 相位

```csharp
// mode: 相位模式
var result = client.SetRFPhase(RFPhaseMode.Positive);  // 正极性
var result = client.SetRFPhase(RFPhaseMode.Negative);  // 负极性
```

| 枚举值 | 值 | 说明 |
|--------|-----|------|
| `RFPhaseMode.Positive` | 0x0001 | 正极性相位 |
| `RFPhaseMode.Negative` | 0x0002 | 负极性相位 |

### 4.5 设置 RF 电流档位

```csharp
// range: 电流档位
var result = client.SetRFCurrentRange(RFCurrentRange.mA2);  // 2mA
var result = client.SetRFCurrentRange(RFCurrentRange.mA5);  // 5mA
```

| 枚举值 | 值 | 说明 |
|--------|-----|------|
| `RFCurrentRange.mA2` | 0x0001 | 2 mA 档位 |
| `RFCurrentRange.mA5` | 0x0002 | 5 mA 档位 |

---

## 五、扫描控制

### 5.1 设置扫描参数

```csharp
var scanParams = new ScanParameters
{
    SegmentCount = 1,      // 片段数 (1~100)
    StartMassNumber = 10,  // 起始质量数 (0~2000)
    StepCount = 50,        // 步数 (0~2000)
    DwellTime = 20         // 驻留时间 (0~16000 ms)
};

var result = client.SetScanParameters(scanParams);
```

| 参数 | 类型 | 范围 | 说明 |
|------|------|------|------|
| SegmentCount | ushort | 1 ~ 100 | 片段数 |
| StartMassNumber | ushort | 0 ~ 2000 | 起始质量数 |
| StepCount | ushort | 0 ~ 2000 | 步数，起始质量数+步数 ≤ 2000 |
| DwellTime | ushort | 0 ~ 16000 | 驻留时间，单位 ms |

### 5.2 开始扫描

```csharp
var scanConfig = new ScanConfig
{
    ScanSegment = 1,           // 扫描片段编号
    LoopCount = 10000,         // 循环次数 (<999为循环次数, ≥999为无限循环)
    SettlingTime = 200,        // 建立时间 (us)
    SegmentInterval = 10000    // 段与段间隔时间 (us)
};

var result = client.StartScan(scanConfig);
```

| 参数 | 类型 | 范围 | 说明 |
|------|------|------|------|
| ScanSegment | ushort | - | 扫描片段编号 |
| LoopCount | ushort | 1 ~ 65535 | 循环次数（999表示无限循环） |
| SettlingTime | ushort | - | 建立时间，单位 us |
| SegmentInterval | ushort | - | 段间隔时间，单位 us |

### 5.3 停止扫描

```csharp
var result = client.StopScan();
```

### 5.4 读取质量扫描数据

```csharp
// 从质量数 27.5 到 28.5，步长 0.1
var scanData = client.ReadMassScanData(27.5, 28.5, 0.1);

if (scanData.Success)
{
    foreach (var point in scanData.Value)
    {
        Console.WriteLine($"质量数: {point.MassNumber}, 峰高: {point.PeakHeight}");
    }
}
```

| 参数 | 类型 | 范围 | 说明 |
|------|------|------|------|
| startMass | double | 0 ~ 200 | 起始质量数 |
| endMass | double | 0 ~ 200 | 终止质量数 |
| stepSize | double | 0.01 ~ | 步长（默认 0.1） |

返回 `List<MZScanData>`，每个元素包含：
- `MassNumber`: 质量数
- `PeakHeight`: 峰高值

---

## 六、灯丝控制

### 6.1 选择灯丝

```csharp
var result = client.SelectFilament(FilamentSelect.FilamentA);  // 灯丝 A
var result = client.SelectFilament(FilamentSelect.FilamentB);  // 灯丝 B
```

| 枚举值 | 值 | 说明 |
|--------|-----|------|
| `FilamentSelect.FilamentA` | 0x0001 | 灯丝 A |
| `FilamentSelect.FilamentB` | 0x0002 | 灯丝 B |

### 6.2 设置灯丝发射电流

```csharp
// currentUA: 发射电流，单位 uA
// 范围: 0 ~ 2000 uA
var result = client.SetFilamentEmission(1000);
```

| 参数 | 类型 | 范围 | 说明 |
|------|------|------|------|
| currentUA | ushort | 0 ~ 2000 | 发射电流，单位 uA |

### 6.3 设置放电时间

```csharp
// timeUs: 放电时间，单位 us
var result = client.SetDischargeTime(200);
```

| 参数 | 类型 | 说明 |
|------|------|------|
| timeUs | ushort | 放电时间，单位 us |

### 6.4 设置灯丝电流安全值

```csharp
// currentA: 安全电流值，单位 A
var result = client.SetFilamentCurrentSafety(4.6);
```

| 参数 | 类型 | 说明 |
|------|------|------|
| currentA | double | 电流安全值，单位 A |

---

## 七、EM 板（检测器）控制

### 7.1 设置 DET_HV 电压

```csharp
// voltage: 电压值 (V)
// 范围: 0 ~ 2200 V
var result = client.SetDET_HVVoltage(200);
```

| 参数 | 类型 | 范围 | 说明 |
|------|------|------|------|
| voltage | ushort | 0 ~ 2200 | DET_HV 电压，单位 V |

### 7.2 设置 Anode 电压

```csharp
// voltage: 电压值 (V)
// 范围: 0 ~ 3000 V
var result = client.SetAnodeVoltage(200);
```

| 参数 | 类型 | 范围 | 说明 |
|------|------|------|------|
| voltage | ushort | 0 ~ 3000 | Anode 电压，单位 V |

### 7.3 设置 Focus 电压

```csharp
// voltage: 电压值 (V)
// 范围: 0 ~ 320 V
var result = client.SetFocusVoltage(10);
```

| 参数 | 类型 | 范围 | 说明 |
|------|------|------|------|
| voltage | ushort | 0 ~ 320 | Focus 电压，单位 V |

### 7.4 设置 Electron Energy

```csharp
// energy: 电子能量值 (eV)
// 范围: 0 ~ 320 eV
var result = client.SetElectronEnergy(70);
```

| 参数 | 类型 | 范围 | 说明 |
|------|------|------|------|
| energy | ushort | 0 ~ 320 | 电子能量，单位 eV |

---

## 八、FPGA 检测器控制

### 8.1 设置检测器 OutB

```csharp
// value: 设置值
// 范围: 0 ~ 4096
var result = client.SetDetectorOutB(700);
```

### 8.2 设置 OutC (Lower)

```csharp
// value: 设置值
// 范围: 0 ~ 4096
var result = client.SetOutCLower(500);
```

### 8.3 设置 OutD (Upper)

```csharp
// value: 设置值
// 范围: 0 ~ 4096
var result = client.SetOutDUpper(3500);
```

---

## 九、系统控制

### 9.1 保存参数

```csharp
var result = client.SaveParameters();
```

### 9.2 设置波特率

```csharp
var result = client.SetBaudRate(BaudRateSetting.Baud9600);    // 9600
var result = client.SetBaudRate(BaudRateSetting.Baud115200);  // 115200
var result = client.SetBaudRate(BaudRateSetting.Baud921600);  // 921600
```

| 枚举值 | 值 | 波特率 |
|--------|-----|--------|
| `BaudRateSetting.Baud9600` | 0x01 | 9600 |
| `BaudRateSetting.Baud115200` | 0x02 | 115200 |
| `BaudRateSetting.Baud921600` | 0x03 | 921600 |

### 9.3 清除错误标记

```csharp
var result = client.ClearErrorFlag();
```

### 9.4 开始校验

```csharp
var result = client.StartCalibration();
```

### 9.5 设置真空系数校正

```csharp
// coefficient: 真空系数值
var result = client.SetVacuumCoefficient(0.000222);
```

---

## 十、读取操作

### 10.1 读取 PS 板心跳

```csharp
var result = client.ReadPSHeartbeat();
if (result.Success)
    Console.WriteLine($"PS心跳: {result.Value}");
```

### 10.2 读取 SYS 板心跳

```csharp
var result = client.ReadSYSHeartbeat();
if (result.Success)
    Console.WriteLine($"SYS心跳: {result.Value}");
```

### 10.3 读取真空度

```csharp
var result = client.ReadVacuumDegree();
if (result.Success)
    Console.WriteLine($"真空度: {result.Value:E2} Torr");
```

### 10.4 读取电压值

| 方法 | 返回类型 | 说明 |
|------|----------|------|
| `ReadAnodeVoltage()` | double | Anode 电压 (V) |
| `ReadEMVoltage()` | double | EM 电压 (V) |
| `ReadFocusVoltage()` | ushort | Focus 电压 (V) |
| `ReadElectronEnergy()` | double | 电子能量 (eV) |

### 10.5 读取电流值

| 方法 | 返回类型 | 说明 |
|------|----------|------|
| `ReadFilamentCurrent()` | double | 灯丝电流 (A) |
| `ReadFilamentEmissionCurrent()` | ushort | 灯丝发射电流 (uA) |
| `ReadAnodeCurrent()` | double | Anode 电流 (A) |
| `ReadEMCurrent()` | double | EM 电流 (A) |

### 10.6 读取 RF 状态

```csharp
// 读取 RF 频率
var freqResult = client.ReadRFFrequency();
if (freqResult.Success)
    Console.WriteLine($"RF频率: {freqResult.Value} Hz");
```

### 10.7 读取设备完整状态

```csharp
var status = client.ReadStatus();
if (status.Success)
{
    Console.WriteLine($"真空度: {status.Value.VacuumDegree:E2}");
    Console.WriteLine($"Anode电压: {status.Value.AnodeVoltage:F2} V");
    Console.WriteLine($"EM电压: {status.Value.EMVoltage:F2} V");
    Console.WriteLine($"灯丝电流: {status.Value.FilamentCurrent:F3} A");
    Console.WriteLine($"RF频率: {status.Value.RF_Fb1} Hz");
}
```

`RGAStatusInfo` 包含以下字段：

| 字段 | 类型 | 说明 |
|------|------|------|
| PSHeartbeat | ushort | PS 板心跳 |
| SYSHeartbeat | ushort | SYS 板心跳 |
| VacuumDegree | double | 真空度 (Torr) |
| AnodeVoltage | double | Anode 电压 (V) |
| EMVoltage | double | EM 电压 (V) |
| FilamentVoltage | double | 灯丝电压 (V) |
| FilamentCurrent | double | 灯丝电流 (A) |
| FilamentEmissionCurrent | double | 灯丝发射电流 (A) |
| FocusVoltage | ushort | Focus 电压 |
| ElectronEnergy | double | 电子能量 (eV) |
| AnodeCurrent | double | Anode 电流 (A) |
| AnodeDriveCurrent | double | Anode 驱动电流 (A) |
| EMCurrent | double | EM 电流 (A) |
| RFPrimaryCurrent | ushort | RF 初级电流 |
| RF_Fb1 | ushort | RF 反馈 1 |
| RF_Fb2 | ushort | RF 反馈 2 |
| AvccVoltage | double | Avcc 电压 24V |
| Vcc1Voltage | double | Vcc1 电压 5V |
| Vcc2Voltage | double | Vcc2 电压 3.3V |
| VPositiveVoltage | double | V+ 电压 15V |
| VNegativeVoltage | double | V- 电压 5V |
| AvccCurrent | double | Avcc 电流 (A) |

---

## 十一、设备初始化

SDK 提供了完整的设备初始化方法，一步完成所有必要配置：

```csharp
// EM 板配置
var emConfig = new EMBoardConfig
{
    DET_HVVoltage = 200,      // DET_HV 电压 (0~2200V)
    AnodeVoltage = 200,       // Anode 电压 (0~3000V)
    FocusVoltage = 10,        // Focus 电压 (0~320V)
    ElectronEnergy = 70      // 电子能量 (0~320eV)
};

// RF 配置
var rfConfig = new RFConfig
{
    Frequency = 2000000,     // RF 频率 (Hz)
    CenterVoltage = 5.0,     // RF 中心电压 (5~12V)
    PhaseMode = RFPhaseMode.Positive,    // 相位模式
    CurrentRange = RFCurrentRange.mA2    // 电流档位
};

// 灯丝配置
var filamentConfig = new FilamentConfig
{
    Filament = FilamentSelect.FilamentA,  // 选择灯丝 A
    EmissionCurrent = 1000,              // 发射电流 (0~2000uA)
    DischargeTime = 200                   // 放电时间 (us)
};

// 执行初始化
var result = client.InitializeDevice(emConfig, rfConfig, filamentConfig);

if (result.Success)
    Console.WriteLine("设备初始化成功！");
else
    Console.WriteLine($"初始化失败: {result.ErrorMessage}");
```

初始化流程会依次执行：
1. 选择灯丝
2. 设置 DET_HV 电压
3. 设置 Anode 电压
4. 设置 Focus 电压
5. 设置 Electron Energy
6. 设置 RF 频率
7. 设置 RF 中心电压
8. 设置 RF 相位
9. 设置 RF 电流档位
10. 设置灯丝发射电流
11. 设置放电时间
12. 保存参数

---

## 十二、操作结果处理

### 12.1 操作结果类

所有设置操作返回 `RGAOperationResult`：

```csharp
public class RGAOperationResult
{
    public bool Success { get; set; }      // 是否成功
    public string? ErrorMessage { get; set; }  // 错误消息
    public byte[]? RawResponse { get; set; }   // 原始响应
}
```

### 12.2 读取结果类

所有读取操作返回 `RGAReadResult<T>`：

```csharp
public class RGAReadResult<T> : RGAOperationResult
{
    public T? Value { get; set; }  // 读取的值
}
```

### 12.3 示例

```csharp
var result = client.SetRFFrequency(2000000);

if (result.Success)
{
    Console.WriteLine("设置成功");
}
else
{
    Console.WriteLine($"设置失败: {result.ErrorMessage}");
}

// 读取操作
var vacuum = client.ReadVacuumDegree();
if (vacuum.Success)
    Console.WriteLine($"真空度: {vacuum.Value}");
else
    Console.WriteLine($"读取失败: {vacuum.ErrorMessage}");
```

---

## 十三、事件处理

### 13.1 日志事件

```csharp
client.LogReceived += (sender, message) =>
{
    Console.WriteLine($"[LOG] {message}");
};
```

### 13.2 错误事件

```csharp
client.ErrorOccurred += (sender, exception) =>
{
    Console.WriteLine($"[ERROR] {exception.Message}");
};
```

### 13.3 日志输出示例

```
[LOG] 发送: 01 10 00 00 00 0A 14 03 0A 01 00 1E 84 80 ...
[LOG] 接收: 01 10 00 00 00 0A 14 03 0A 01 00 1E 84 80 ...
```

---

## 十四、完整示例

### 14.1 基本操作示例

```csharp
using RGASDK;

class Program
{
    static void Main()
    {
        // 创建客户端
        using var client = new RGACLient("COM3", 115200);

        // 设置事件处理
        client.LogReceived += (s, msg) => Console.WriteLine($"[LOG] {msg}");
        client.ErrorOccurred += (s, ex) => Console.WriteLine($"[ERROR] {ex.Message}");

        // 设置从机地址
        client.SlaveAddress = 1;

        // 读取设备状态
        Console.WriteLine("\n=== 读取设备状态 ===");
        var vacuum = client.ReadVacuumDegree();
        Console.WriteLine($"真空度: {vacuum.Value:E2} Torr");

        var psBeat = client.ReadPSHeartbeat();
        Console.WriteLine($"PS心跳: {psBeat.Value}");

        // 设置 RF 参数
        Console.WriteLine("\n=== 设置 RF 参数 ===");
        client.SetRFFrequency(2000000);      // 2 MHz
        client.SetRFCenterVoltage(10.0);     // 10 V
        client.SetRFPhase(RFPhaseMode.Positive);
        client.SetRFCurrentRange(RFCurrentRange.mA2);

        // 设置检测器
        Console.WriteLine("\n=== 设置检测器 ===");
        client.SetAnodeVoltage(200);
        client.SetFocusVoltage(10);
        client.SetElectronEnergy(70);

        // 设置灯丝
        Console.WriteLine("\n=== 设置灯丝 ===");
        client.SelectFilament(FilamentSelect.FilamentA);
        client.SetFilamentEmission(1000);

        // 保存参数
        client.SaveParameters();

        Console.WriteLine("\n=== 完成 ===");
    }
}
```

### 14.2 扫描操作示例

```csharp
// 配置扫描参数
var scanParams = new ScanParameters
{
    SegmentCount = 1,
    StartMassNumber = 10,
    StepCount = 50,
    DwellTime = 20
};
client.SetScanParameters(scanParams);

// 开始扫描
var scanConfig = new ScanConfig
{
    ScanSegment = 1,
    LoopCount = 10,
    SettlingTime = 200,
    SegmentInterval = 10000
};
client.StartScan(scanConfig);

// 读取扫描数据
Thread.Sleep(1000);  // 等待扫描完成
var scanData = client.ReadMassScanData(18, 19, 0.1);

foreach (var point in scanData.Value)
{
    Console.WriteLine($"质量数: {point.MassNumber}, 峰高: {point.PeakHeight}");
}

// 停止扫描
client.StopScan();
```

### 14.3 完整设备初始化示例

```csharp
// 创建配置对象
var emConfig = new EMBoardConfig
{
    DET_HVVoltage = 200,
    AnodeVoltage = 200,
    FocusVoltage = 10,
    ElectronEnergy = 70
};

var rfConfig = new RFConfig
{
    Frequency = 2000000,
    CenterVoltage = 5.0,
    PhaseMode = RFPhaseMode.Positive,
    CurrentRange = RFCurrentRange.mA2
};

var filamentConfig = new FilamentConfig
{
    Filament = FilamentSelect.FilamentA,
    EmissionCurrent = 1000,
    DischargeTime = 200
};

// 执行初始化
var result = client.InitializeDevice(emConfig, rfConfig, filamentConfig);

if (result.Success)
{
    Console.WriteLine("设备初始化成功！");

    // 验证初始化结果
    var status = client.ReadStatus();
    Console.WriteLine($"真空度: {status.Value.VacuumDegree:E2}");
    Console.WriteLine($"RF频率: {status.Value.RF_Fb1} Hz");
}
else
{
    Console.WriteLine($"初始化失败: {result.ErrorMessage}");
}
```

---

## 十五、错误代码

| 错误类型 | 说明 |
|----------|------|
| 连接失败 | 无法打开串口或设备无响应 |
| CRC 校验失败 | 数据传输错误 |
| 超时 | 设备响应超时 |
| 参数越界 | 设置参数超出有效范围 |

---

## 十六、联系方式

如有问题，请联系技术支持。

---

*文档版本 1.0.0 - 2026-05-26*
