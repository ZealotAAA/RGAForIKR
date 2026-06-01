namespace RGASDK;

/// <summary>
/// 扫描参数配置
/// </summary>
public class ScanParameters
{
    /// <summary>
    /// 片段数 (1~100)
    /// </summary>
    public ushort SegmentCount { get; set; } = 1;

    /// <summary>
    /// 起始质量数 (0~2000)
    /// </summary>
    public ushort StartMassNumber { get; set; } = 10;

    /// <summary>
    /// 步数 (0~2000), 起始质量数+步数 ≤ 2000
    /// </summary>
    public ushort StepCount { get; set; } = 50;

    /// <summary>
    /// 驻留时间 (0~16000 ms)
    /// </summary>
    public ushort DwellTime { get; set; } = 20;

    /// <summary>
    /// 验证参数是否有效
    /// </summary>
    public bool IsValid() => SegmentCount >= 1 && SegmentCount <= 100 &&
                               StartMassNumber <= 2000 &&
                               StepCount <= 2000 &&
                               StartMassNumber + StepCount <= 2000 &&
                               DwellTime <= 16000;
}

/// <summary>
/// 扫描配置
/// </summary>
public class ScanConfig
{
    /// <summary>
    /// 扫描片段编号
    /// </summary>
    public ushort ScanSegment { get; set; } = 1;

    /// <summary>
    /// 扫描循环次数 (小于999为循环次数, 999~65536为无限循环)
    /// </summary>
    public ushort LoopCount { get; set; } = 10000;

    /// <summary>
    /// 建立时间 (us)
    /// </summary>
    public ushort SettlingTime { get; set; } = 200;

    /// <summary>
    /// 段与段间隔时间 (us)
    /// </summary>
    public ushort SegmentInterval { get; set; } = 10000;
}

/// <summary>
/// RF配置
/// </summary>
public class RFConfig
{
    /// <summary>
    /// RF频率 (Hz)
    /// </summary>
    public uint Frequency { get; set; } = 2000000;

    /// <summary>
    /// 中心电压 (5~12V)
    /// </summary>
    public double CenterVoltage { get; set; } = 5.0;

    /// <summary>
    /// RF相位模式
    /// </summary>
    public RFPhaseMode PhaseMode { get; set; } = RFPhaseMode.Positive;

    /// <summary>
    /// RF电流档位
    /// </summary>
    public RFCurrentRange CurrentRange { get; set; } = RFCurrentRange.mA2;

    /// <summary>
    /// RF电压使能状态
    /// </summary>
    public bool RFVoltageEnabled { get; set; } = false;
}

/// <summary>
/// 灯丝配置
/// </summary>
public class FilamentConfig
{
    /// <summary>
    /// 灯丝选择
    /// </summary>
    public FilamentSelect Filament { get; set; } = FilamentSelect.FilamentA;

    /// <summary>
    /// 发射电流 (0~2000 uA)
    /// </summary>
    public ushort EmissionCurrent { get; set; } = 1000;

    /// <summary>
    /// 放电时间 (us)
    /// </summary>
    public ushort DischargeTime { get; set; } = 200;

    /// <summary>
    /// 电流安全值 (A)
    /// </summary>
    public double CurrentSafetyValue { get; set; } = 4.6;
}

/// <summary>
/// EM板(检测器)配置
/// </summary>
public class EMBoardConfig
{
    /// <summary>
    /// DET_HV电压 (0~2200V)
    /// </summary>
    public ushort DET_HVVoltage { get; set; } = 200;

    /// <summary>
    /// Anode电压 (0~3000V)
    /// </summary>
    public ushort AnodeVoltage { get; set; } = 200;

    /// <summary>
    /// Focus电压 (0~320V)
    /// </summary>
    public ushort FocusVoltage { get; set; } = 10;

    /// <summary>
    /// Electron能量 (0~320 EV)
    /// </summary>
    public ushort ElectronEnergy { get; set; } = 70;
}

/// <summary>
/// FPGA检测器配置
/// </summary>
public class FPGADetectorConfig
{
    /// <summary>
    /// OutB值 (0~4096)
    /// </summary>
    public ushort OutB { get; set; } = 700;

    /// <summary>
    /// OutC Lower值 (0~4096)
    /// </summary>
    public ushort OutCLower { get; set; } = 500;

    /// <summary>
    /// OutD Upper值 (0~4096)
    /// </summary>
    public ushort OutDUpper { get; set; } = 3500;
}

/// <summary>
/// 真空系数校正参数
/// </summary>
public class VacuumCalibrationParams
{
    /// <summary>
    /// 真空系数值 (如 2.22e-4 表示为 0.000222)
    /// </summary>
    public double Coefficient { get; set; } = 0.000222;

    /// <summary>
    /// 参数1
    /// </summary>
    public ushort Param1 { get; set; } = 0x0001;

    /// <summary>
    /// 参数2 (低16位)
    /// </summary>
    public ushort Param2 { get; set; } = 0xC8AC;

    /// <summary>
    /// 参数3 (高16位)
    /// </summary>
    public ushort Param3 { get; set; } = 0x3968;
}

/// <summary>
/// 操作结果基类
/// </summary>
public class RGAOperationResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 原始响应数据
    /// </summary>
    public byte[]? RawResponse { get; set; }

    public static RGAOperationResult Ok(byte[]? rawResponse = null) =>
        new() { Success = true, RawResponse = rawResponse };

    public static RGAOperationResult Fail(string message, byte[]? rawResponse = null) =>
        new() { Success = false, ErrorMessage = message, RawResponse = rawResponse };
}

/// <summary>
/// 读取数据结果
/// </summary>
public class RGAReadResult<T> : RGAOperationResult
{
    /// <summary>
    /// 读取到的值
    /// </summary>
    public T? Value { get; set; }

    public static RGAReadResult<T> Ok(T value, byte[]? rawResponse = null) =>
        new() { Success = true, Value = value, RawResponse = rawResponse };

    public static new RGAReadResult<T> Fail(string message, byte[]? rawResponse = null) =>
        new() { Success = false, ErrorMessage = message, RawResponse = rawResponse };
}

/// <summary>
/// 设备状态信息
/// </summary>
public class RGAStatusInfo
{
    public ushort PSHeartbeat { get; set; }
    public ushort SYSHeartbeat { get; set; }
    public double AnodeVoltage { get; set; }
    public double EMVoltage { get; set; }
    public double FilamentEmissionCurrent { get; set; }
    public double FocusVoltage { get; set; }
    public double ElectronEnergy { get; set; }
    public double FilamentVoltage { get; set; }
    public double FilamentCurrent { get; set; }
    public double AnodeCurrent { get; set; }
    public double AnodeDriveCurrent { get; set; }
    public double EMCurrent { get; set; }
    public double RFPrimaryCurrent { get; set; }
    public double RF_Fb1 { get; set; }
    public double RF_Fb2 { get; set; }
    public double VacuumDegree { get; set; }
    public double AvccVoltage { get; set; }
    public double Vcc2Voltage { get; set; }
    public double Vcc1Voltage { get; set; }
    public double VNegativeVoltage { get; set; }
    public double VPositiveVoltage { get; set; }
    public double AvccCurrent { get; set; }
    public int BaudRate { get; set; }
}

/// <summary>
/// m/Z 扫描数据
/// </summary>
public class MZScanData
{
    /// <summary>
    /// 质量数
    /// </summary>
    public double MassNumber { get; set; }

    /// <summary>
    /// 峰高值
    /// </summary>
    public double PeakHeight { get; set; }
}

/// <summary>
/// RGA设备信息
/// </summary>
public class RGADeviceInfo
{
    /// <summary>
    /// 从机地址
    /// </summary>
    public byte SlaveAddress { get; set; }

    /// <summary>
    /// 固件版本
    /// </summary>
    public string? FirmwareVersion { get; set; }

    /// <summary>
    /// 设备型号
    /// </summary>
    public string? Model { get; set; }
}
