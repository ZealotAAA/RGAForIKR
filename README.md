# RGASDK - RGA残余气体分析仪 Modbus RTU SDK

基于RGA指令集文档封装的C# SDK，用于与RGA设备进行Modbus RTU通信。

## 项目结构

```
RGASDK/
├── Enums.cs            # 枚举和常量定义
├── Models.cs           # 数据模型
├── Commands.cs         # 命令参数转换器
├── ProtocolHandler.cs   # Modbus协议处理器
├── Communicator.cs     # 串口通信器
├── RGACLient.cs        # 高级API客户端
├── Program.cs          # 示例程序入口
├── RGASDK.csproj       # 项目文件
└── README.md           # 本文档
```

## 快速开始

### 安装依赖

```bash
dotnet restore
```

### 运行示例

```bash
dotnet run
```

## 使用方法

### 1. 基本连接

```csharp
using RGASDK;

// 方式1: 直接连接
using var client = new RGACLient("COM3", 115200);

// 方式2: 先创建通信器再连接
using var communicator = new RGACommunicator();
communicator.Connect("COM3", 115200);
using var client = new RGACLient(communicator);
```

### 2. RF控制

```csharp
// 设置RF频率 (Hz)
client.SetRFFrequency(2000000); // 2MHz

// 设置RF中心电压 (5~12V)
client.SetRFCenterVoltage(10.0);

// 设置RF相位
client.SetRFPhase(RFPhaseMode.Positive);

// 设置RF电流档位
client.SetRFCurrentRange(RFCurrentRange.mA2);

// 使能/禁用RF电压
client.EnableRFVoltage();
client.DisableRFVoltage();
```

### 3. 检测器控制

```csharp
// 设置Anode电压 (0~3000V)
client.SetAnodeVoltage(200);

// 设置Focus电压 (0~320V)
client.SetFocusVoltage(10);

// 设置ElectronEnergy (0~320 EV)
client.SetElectronEnergy(70);

// 设置DET_HV电压 (0~2200V)
client.SetDET_HVVoltage(200);
```

### 4. 灯丝控制

```csharp
// 选择灯丝
client.SelectFilament(FilamentSelect.FilamentA);

// 设置发射电流 (0~2000 uA)
client.SetFilamentEmission(1000);

// 设置放电时间 (us)
client.SetDischargeTime(200);

// 设置灯丝电流安全值 (A)
client.SetFilamentCurrentSafety(4.6);
```

### 5. 扫描控制

```csharp
// 设置扫描参数
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
    LoopCount = 10000,  // 999以上为无限循环
    SettlingTime = 200,
    SegmentInterval = 10000
};
client.StartScan(scanConfig);

// 停止扫描
client.StopScan();
```

### 6. 读取数据

```csharp
// 读取真空度
var vacuum = client.ReadVacuumDegree();
Console.WriteLine($"真空度: {vacuum.Value:E2} Torr");

// 读取Anode电压
var anode = client.ReadAnodeVoltage();
Console.WriteLine($"Anode电压: {anode.Value:F2} V");

// 读取灯丝电流
var filament = client.ReadFilamentCurrent();
Console.WriteLine($"灯丝电流: {filament.Value:F2} A");

// 读取ElectronEnergy
var energy = client.ReadElectronEnergy();
Console.WriteLine($"ElectronEnergy: {energy.Value:F2} EV");

// 读取PS/SYS板心跳
var psHeartbeat = client.ReadPSHeartbeat();
var sysHeartbeat = client.ReadSYSHeartbeat();

// 读取设备完整状态
var status = client.ReadStatus();
if (status.Success && status.Value != null)
{
    Console.WriteLine($"真空度: {status.Value.VacuumDegree:E2}");
    Console.WriteLine($"Anode电压: {status.Value.AnodeVoltage:F2}");
}
```

### 7. 质量扫描数据读取

```csharp
// 读取质量数27.5到28.5的波形数据，步长0.1
var scanResult = client.ReadMassScanData(27.5, 28.5, 0.1);
if (scanResult.Success && scanResult.Value != null)
{
    foreach (var point in scanResult.Value)
    {
        Console.WriteLine($"m/z={point.MassNumber:F1}: {point.PeakHeight}");
    }
}
```

### 8. 设备完整初始化

```csharp
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

var result = client.InitializeDevice(emConfig, rfConfig, filamentConfig);
Console.WriteLine($"初始化结果: {(result.Success ? "成功" : result.ErrorMessage)}");
```

### 9. 系统控制

```csharp
// 保存参数
client.SaveParameters();

// 清除错误标记
client.ClearErrorFlag();

// 开始校验
client.StartCalibration();

// 设置真空系数校正
client.SetVacuumCoefficient(0.000222);

// 设置波特率
client.SetBaudRate(BaudRateSetting.Baud115200);
```

### 10. 低层通信接口

```csharp
using var communicator = new RGACommunicator();
communicator.Connect("COM3");

// 获取可用串口
var ports = RGACommunicator.GetAvailablePorts();

// 配置重试参数
communicator.RetryCount = 3;
communicator.RetryDelay = 100;
communicator.ReadTimeout = 1000;

// 订阅日志
communicator.LogReceived += (s, msg) => Console.WriteLine($"[LOG] {msg}");
communicator.ErrorOccurred += (s, ex) => Console.WriteLine($"[ERROR] {ex.Message}");

// 直接构建命令
var data = RGACommands.SetRFFrequency(2000000);
bool success = communicator.SendWriteCommand(
    (ushort)RGASubystemAddress.ControlBoard,
    (ushort)ControlBoardFunctionCode.SetRFFrequency,
    data);

// 发送读命令
var values = communicator.SendReadCommand(RGAHoldingRegisters.VacuumDegree, 2);
```

## 枚举说明

### 子系统地址 (RGASubsystemAddress)

| 值 | 名称 | 说明 |
|---|---|---|
| 0x0001 | EMBoard | EM板 (检测器) |
| 0x0002 | FPGABoard | FPGA板 |
| 0x0003 | ControlBoard | 控制板 |

### RF相位模式 (RFPhaseMode)

| 值 | 名称 | 说明 |
|---|---|---|
| 0x0001 | Positive | 正极性相位 |
| 0x0002 | Negative | 负极性相位 |

### RF电流档位 (RFCurrentRange)

| 值 | 名称 | 说明 |
|---|---|---|
| 0x0001 | mA2 | 2mA |
| 0x0002 | mA5 | 5mA |

### 灯丝选择 (FilamentSelect)

| 值 | 名称 | 说明 |
|---|---|---|
| 0x0001 | FilamentA | 灯丝A |
| 0x0002 | FilamentB | 灯丝B |

### 波特率设置 (BaudRateSetting)

| 值 | 名称 | 说明 |
|---|---|---|
| 0x01 | Baud9600 | 9600 |
| 0x02 | Baud115200 | 115200 |
| 0x03 | Baud921600 | 921600 |

## 参数转换公式

| 参数 | 公式 |
|------|------|
| RF中心电压 | 下发参数 = (电压-5) / 44.2 * 28 * 65535 / 5 |
| RF频率 | 数据1 = 频率>>16, 数据2 = 频率&0xFFFF |
| Anode电压 | 下发参数 = 电压 * 10 |
| 驻留时间 | 下发参数 = 毫秒 * 500 |
| 放电时间 | 下发参数 = 微秒 * 100 |
| 建立时间 | 下发参数 = 微秒 * 100 |
| ElectronEnergy | 显示值 = 读取值 * 5 / 64.8 |

## 注意事项

1. 默认串口参数: 115200波特率, 8数据位, 无校验, 1停止位
2. 从机地址默认值为1
3. 写入操作自动添加CRC16校验
4. 读取操作自动验证CRC16校验
5. 所有操作支持重试机制 (默认3次)
6. 使用`using`语句确保资源正确释放

## 许可证

MIT License
