using System;

namespace RGASDK;

/// <summary>
/// RGA命令参数转换器
/// 根据指令集文档进行单位转换和参数计算
/// </summary>
public static class RGACommands
{
    #region RF相关命令

    /// <summary>
    /// 设置RF频率
    /// 文档帧: 00 1E 84 80 -> data[0]=0x1E00, data[1]=0x8480
    /// 帧添加: data[0]高字节, data[0]低字节, data[1]高字节, data[1]低字节
    /// 频率 = 0x1E8480 = 2000000Hz
    /// </summary>
    public static ushort[] SetRFFrequency(uint frequencyHz)
    {
        return new ushort[]
        {
            (ushort)(frequencyHz >> 16),     // 高16位 -> 帧中00 1E
            (ushort)(frequencyHz & 0xFFFF)    // 低16位 -> 帧中84 80
        };
    }

    /// <summary>
    /// 计算RF中心电压的下发参数
    /// 公式: 下发参数 = (输入值-5) / 44.2 * 28 * 65535 / 5
    /// </summary>
    public static ushort[] SetRFCenterVoltage(double voltage)
    {
        if (voltage < 5 || voltage > 12)
            throw new ArgumentOutOfRangeException(nameof(voltage), "电压范围: 5~12V");

        double param = (voltage - 5) / 44.2 * 28 * 65535 / 5;
        return new ushort[] { (ushort)Math.Round(param) };
    }

    #endregion

    #region 扫描参数命令

    /// <summary>
    /// 设置扫描参数
    /// 文档示例: 片段数1, 起始质量数10, 步数50, 驻留时间20ms
    /// 下发参数: 数据1=0x0001, 数据2=0x000A, 数据3=0x0032, 数据4=0x0000, 数据5=0x2710
    /// 驻留时间下发参数 = 输入值 * 500
    /// </summary>
    public static ushort[] SetScanParameters(ushort segmentCount, ushort startMass, ushort stepCount, ushort dwellTime)
    {
        if (segmentCount < 1 || segmentCount > 100)
            throw new ArgumentOutOfRangeException(nameof(segmentCount), "片段数范围: 1~100");
        if (startMass > 2000)
            throw new ArgumentOutOfRangeException(nameof(startMass), "起始质量数范围: 0~2000");
        if (stepCount > 2000)
            throw new ArgumentOutOfRangeException(nameof(stepCount), "步数范围: 0~2000");
        if (startMass + stepCount > 2000)
            throw new ArgumentOutOfRangeException(nameof(startMass), "起始质量数+步数 ≤ 2000");
        if (dwellTime > 16000)
            throw new ArgumentOutOfRangeException(nameof(dwellTime), "驻留时间范围: 0~16000 ms");

        ushort dwellTimeParam = (ushort)(dwellTime * 500);

        return new ushort[]
        {
            segmentCount,     // 数据1: 片段数
            startMass,       // 数据2: 起始质量数
            stepCount,       // 数据3: 步数
            0x0000,          // 数据4: 预留
            dwellTimeParam   // 数据5: 驻留时间 * 500
        };
    }

    /// <summary>
    /// 设置扫描参数 (使用ScanParameters对象)
    /// </summary>
    public static ushort[] SetScanParameters(ScanParameters scanParams)
    {
        return SetScanParameters(scanParams.SegmentCount, scanParams.StartMassNumber,
            scanParams.StepCount, scanParams.DwellTime);
    }

    /// <summary>
    /// 扫描开始参数
    /// 文档示例: 扫描片段1, 循环次数10000, 建立时间200us, 间隔10000us
    /// 下发参数: 数据1=0x0001, 数据2=0x2710, 数据3=0x0000, 数据4=0x4E20, 数据5=0x0000, 数据6=0x2710
    /// 建立时间下发参数 = 输入值 * 100
    /// 间隔时间下发参数 = 输入值 * 100
    /// </summary>
    public static ushort[] ScanStart(ushort scanSegment, ushort loopCount, ushort settlingTime, ushort segmentInterval)
    {
        ushort settlingTimeParam = (ushort)(settlingTime * 100);
        ushort intervalParam = (ushort)(segmentInterval * 100);

        return new ushort[]
        {
            scanSegment,          // 数据1: 扫描片段
            0x2710,              // 数据2: 预留(0x2710=10000)
            0x0000,              // 数据3: 预留
            settlingTimeParam,   // 数据4: 建立时间 * 100
            0x0000,              // 数据5: 预留
            intervalParam        // 数据6: 间隔时间 * 100
        };
    }

    /// <summary>
    /// 扫描开始参数 (使用ScanConfig对象)
    /// </summary>
    public static ushort[] ScanStart(ScanConfig config)
    {
        return ScanStart(config.ScanSegment, config.LoopCount, config.SettlingTime, config.SegmentInterval);
    }

    #endregion

    #region EM板(检测器)命令

    /// <summary>
    /// 设置DET_HV电压
    /// </summary>
    public static ushort[] SetDET_HVVoltage(ushort voltage)
    {
        if (voltage > 2200)
            throw new ArgumentOutOfRangeException(nameof(voltage), "DET_HV电压范围: 0~2200V");

        return new ushort[] { voltage };
    }

    /// <summary>
    /// 设置Anode电压
    /// </summary>
    public static ushort[] SetAnodeVoltage(ushort voltage)
    {
        if (voltage > 3000)
            throw new ArgumentOutOfRangeException(nameof(voltage), "Anode电压范围: 0~3000V");

        return new ushort[] { (ushort)(voltage * 10) };
    }

    /// <summary>
    /// 设置Focus电压
    /// </summary>
    public static ushort[] SetFocusVoltage(ushort voltage)
    {
        if (voltage > 320)
            throw new ArgumentOutOfRangeException(nameof(voltage), "Focus电压范围: 0~320V");

        return new ushort[] { voltage };
    }

    /// <summary>
    /// 设置ElectronEnergy
    /// </summary>
    public static ushort[] SetElectronEnergy(ushort energy)
    {
        if (energy > 320)
            throw new ArgumentOutOfRangeException(nameof(energy), "ElectronEnergy范围: 0~320 EV");

        return new ushort[] { energy };
    }

    /// <summary>
    /// 设置灯丝发射电流
    /// </summary>
    public static ushort[] SetFilamentEmission(ushort currentUA)
    {
        if (currentUA > 2000)
            throw new ArgumentOutOfRangeException(nameof(currentUA), "发射电流范围: 0~2000 uA");

        return new ushort[] { currentUA };
    }

    /// <summary>
    /// 设置灯丝电流安全值
    /// </summary>
    public static ushort[] SetFilamentCurrentSafety(double currentA)
    {
        uint rawValue = ConvertToIEEE754Single(currentA);
        return new ushort[]
        {
            (ushort)(rawValue & 0xFFFF),
            (ushort)(rawValue >> 16)
        };
    }

    #endregion

    #region FPGA检测器命令

    /// <summary>
    /// 设置检测器OutB
    /// </summary>
    public static ushort[] SetDetectorOutB(ushort value)
    {
        if (value > 4096)
            throw new ArgumentOutOfRangeException(nameof(value), "OutB范围: 0~4096");

        return new ushort[] { value };
    }

    /// <summary>
    /// 设置OutC (Lower)
    /// </summary>
    public static ushort[] SetOutCLower(ushort value)
    {
        if (value > 4096)
            throw new ArgumentOutOfRangeException(nameof(value), "OutC范围: 0~4096");

        return new ushort[] { value };
    }

    /// <summary>
    /// 设置OutD (Upper)
    /// </summary>
    public static ushort[] SetOutDUpper(ushort value)
    {
        if (value > 4096)
            throw new ArgumentOutOfRangeException(nameof(value), "OutD范围: 0~4096");

        return new ushort[] { value };
    }

    #endregion

    #region 放电时间

    /// <summary>
    /// 设置放电时间
    /// </summary>
    public static ushort[] SetDischargeTime(ushort timeUs)
    {
        ushort param = (ushort)(timeUs * 100);
        return new ushort[] { 0x0000, param };
    }

    #endregion

    #region 真空系数校正

    /// <summary>
    /// 计算真空系数校正参数
    /// </summary>
    public static ushort[] SetVacuumCoefficient(double coefficient)
    {
        uint rawValue = ConvertToIEEE754Single(coefficient);

        return new ushort[]
        {
            0x0001,
            (ushort)(rawValue & 0xFFFF),
            (ushort)(rawValue >> 16)
        };
    }

    #endregion

    #region 辅助方法

    private static uint ConvertToIEEE754Single(double value)
    {
        byte[] bytes = BitConverter.GetBytes((float)value);
        return BitConverter.ToUInt32(bytes, 0);
    }

    /// <summary>
    /// 将IEEE 754单精度浮点数的位表示转换为double
    /// </summary>
    public static double ConvertFromIEEE754Single(ushort low, ushort high)
    {
        uint rawValue = ((uint)high << 16) | low;
        byte[] bytes = BitConverter.GetBytes(rawValue);
        return BitConverter.ToSingle(bytes, 0);
    }

    /// <summary>
    /// 将两个ushort转换为uint
    /// </summary>
    public static uint ToUInt32(ushort high, ushort low)
    {
        return ((uint)high << 16) | low;
    }

    /// <summary>
    /// 将uint拆分为两个ushort
    /// </summary>
    public static (ushort High, ushort Low) FromUInt32(uint value)
    {
        return ((ushort)(value >> 16), (ushort)(value & 0xFFFF));
    }

    #endregion

    #region 读取结果转换

    /// <summary>
    /// 转换灯丝电流 (安培)
    /// Modbus寄存器小端序排列，IEEE 754需要字节交换
    /// </summary>
    public static double ConvertFilamentCurrent(ushort reg0, ushort reg1)
    {
        // reg0=低字节在前寄存器0, reg1=低字节在前寄存器1
        // IEEE 754 小端序: [reg1高字节, reg1低字节, reg0高字节, reg0低字节]
        uint raw = ((uint)reg1 << 16) | reg0;
        return BitConverter.ToSingle(BitConverter.GetBytes(raw), 0);
    }

    /// <summary>
    /// 转换真空度 (Torr)
    /// </summary>
    public static double ConvertVacuumDegree(ushort reg0, ushort reg1)
    {
        uint raw = ((uint)reg1 << 16) | reg0;
        return BitConverter.ToSingle(BitConverter.GetBytes(raw), 0);
    }

    /// <summary>
    /// 转换电压值 (Anode/EM)
    /// </summary>
    public static double ConvertVoltage(ushort reg0, ushort reg1)
    {
        uint raw = ((uint)reg1 << 16) | reg0;
        return BitConverter.ToSingle(BitConverter.GetBytes(raw), 0);
    }

    /// <summary>
    /// 转换电流值
    /// </summary>
    public static double ConvertCurrent(ushort reg0, ushort reg1)
    {
        uint raw = ((uint)reg1 << 16) | reg0;
        return BitConverter.ToSingle(BitConverter.GetBytes(raw), 0);
    }

    /// <summary>
    /// 转换ElectronEnergy显示值
    /// 文档公式: 显示值 = 读取值 * 5 / 64.8
    /// </summary>
    public static double ConvertElectronEnergyDisplay(ushort value)
    {
        return value * 5.0 / 64.8;
    }

    /// <summary>
    /// 从寄存器值读取RF频率 (Hz)
    /// </summary>
    public static uint ReadRFFrequency(ushort high, ushort low)
    {
        return ((uint)high << 16) | low;
    }

    #endregion

}
