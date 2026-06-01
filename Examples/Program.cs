using System;
using RGASDK;

namespace RGASDK.Examples;

/// <summary>
/// RGA SDK 使用示例
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== RGA SDK 示例程序 ===\n");

        // 获取可用串口
        var ports = RGACommunicator.GetAvailablePorts();
        Console.WriteLine($"可用串口: {(ports.Length > 0 ? string.Join(", ", ports) : "无")}");

        if (ports.Length == 0)
        {
            Console.WriteLine("未找到可用串口，请连接RGA设备后重试");
            return;
        }

        // 使用第一个可用串口
        string portName = ports[0];
        Console.WriteLine($"\n使用串口: {portName}\n");

        using var client = new RGACLient(portName, 921600);

        // 订阅日志事件
        client.LogReceived += (s, msg) => Console.WriteLine($"[LOG] {msg}");
        client.ErrorOccurred += (s, ex) => Console.WriteLine($"[ERROR] {ex.Message}");

        // 设置从机地址
        client.SlaveAddress = 1;

        // ===== 基本操作示例 =====
        Console.WriteLine("\n=== 基本操作 ===");

        // 读取设备状态
        Console.WriteLine("\n--- 读取设备状态 ---");
        var psHeartbeat = client.ReadPSHeartbeat();
        Console.WriteLine($"PS心跳: {(psHeartbeat.Success ? psHeartbeat.Value.ToString() : psHeartbeat.ErrorMessage)}");

        var sysHeartbeat = client.ReadSYSHeartbeat();
        Console.WriteLine($"SYS心跳: {(sysHeartbeat.Success ? sysHeartbeat.Value.ToString() : sysHeartbeat.ErrorMessage)}");

        // 读取真空度
        var vacuum = client.ReadVacuumDegree();
        Console.WriteLine($"真空度: {(vacuum.Success ? $"{vacuum.Value:E2} Torr" : vacuum.ErrorMessage)}");

        // ===== RF控制示例 =====
        Console.WriteLine("\n--- RF控制 ---");

        // 设置RF频率
        var result = client.SetRFFrequency(2000000);
        Console.WriteLine($"设置RF频率(2MHz): {(result.Success ? "成功" : result.ErrorMessage)}");

        // 设置RF中心电压
        result = client.SetRFCenterVoltage(10.0);
        Console.WriteLine($"设置RF中心电压(10V): {(result.Success ? "成功" : result.ErrorMessage)}");

        // 设置RF相位
        result = client.SetRFPhase(RFPhaseMode.Positive);
        Console.WriteLine($"设置RF相位(正极性): {(result.Success ? "成功" : result.ErrorMessage)}");

        // ===== 检测器控制示例 =====
        Console.WriteLine("\n--- 检测器控制 ---");

        // 设置Anode电压
        result = client.SetAnodeVoltage(200);
        Console.WriteLine($"设置Anode电压(200V): {(result.Success ? "成功" : result.ErrorMessage)}");

        // 设置Focus电压
        result = client.SetFocusVoltage(10);
        Console.WriteLine($"设置Focus电压(10V): {(result.Success ? "成功" : result.ErrorMessage)}");

        // 设置ElectronEnergy
        result = client.SetElectronEnergy(70);
        Console.WriteLine($"设置ElectronEnergy(70EV): {(result.Success ? "成功" : result.ErrorMessage)}");

        // ===== 灯丝控制示例 =====
        Console.WriteLine("\n--- 灯丝控制 ---");

        // 选择灯丝
        result = client.SelectFilament(FilamentSelect.FilamentA);
        Console.WriteLine($"选择灯丝A: {(result.Success ? "成功" : result.ErrorMessage)}");

        // 设置发射电流
        result = client.SetFilamentEmission(1000);
        Console.WriteLine($"设置发射电流(1000uA): {(result.Success ? "成功" : result.ErrorMessage)}");

        // ===== 扫描控制示例 =====
        Console.WriteLine("\n--- 扫描控制 ---");

        // 设置扫描参数
        var scanParams = new ScanParameters
        {
            SegmentCount = 1,
            StartMassNumber = 10,
            StepCount = 50,
            DwellTime = 20
        };
        result = client.SetScanParameters(scanParams);
        Console.WriteLine($"设置扫描参数: {(result.Success ? "成功" : result.ErrorMessage)}");

        // 保存参数
        result = client.SaveParameters();
        Console.WriteLine($"保存参数: {(result.Success ? "成功" : result.ErrorMessage)}");

        // ===== 设备完整初始化示例 =====
        Console.WriteLine("\n=== 设备完整初始化示例 ===");

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

        result = client.InitializeDevice(emConfig, rfConfig, filamentConfig);
        Console.WriteLine($"设备初始化: {(result.Success ? "成功" : result.ErrorMessage)}");

        // ===== 参数转换示例 =====
        Console.WriteLine("\n=== 参数转换示例 ===");

        var voltageParams = RGACommands.SetRFCenterVoltage(10.0);
        Console.WriteLine($"RF中心电压10V -> 下发参数: 0x{voltageParams[0]:X4}");

        var freqParams = RGACommands.SetRFFrequency(2000000);
        Console.WriteLine($"RF频率2MHz -> 数据1:0x{freqParams[0]:X4}, 数据2:0x{freqParams[1]:X4}");

        Console.WriteLine("\n=== 示例程序结束 ===");
    }
}
