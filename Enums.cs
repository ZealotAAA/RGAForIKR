namespace RGASDK;

/// <summary>
/// RGA子系统地址定义
/// </summary>
public enum RGASubsystemAddress : ushort
{
    /// <summary>EM板 (检测器)</summary>
    EMBoard = 0x0001,

    /// <summary>FPGA板</summary>
    FPGABoard = 0x0002,

    /// <summary>控制板 (主要控制)</summary>
    ControlBoard = 0x0003
}

/// <summary>
/// 控制板子系统功能码
/// </summary>
public enum ControlBoardFunctionCode : ushort
{
    /// <summary>设置RF频率</summary>
    SetRFFrequency = 0x000A,

    /// <summary>RF中心电压</summary>
    SetRFCenterVoltage = 0x000C,

    /// <summary>禁用RF电压</summary>
    DisableRFVoltage = 0x000F,

    /// <summary>使能RF电压</summary>
    EnableRFVoltage = 0x0010,

    /// <summary>设置扫描参数</summary>
    SetScanParameters = 0x0011,

    /// <summary>扫描开始</summary>
    ScanStart = 0x0012,

    /// <summary>停止扫描</summary>
    ScanStop = 0x0013,

    /// <summary>RF相位设置</summary>
    SetRFPhase = 0x001B,

    /// <summary>RF电流档位</summary>
    SetRF10or5mA = 0x001C,

    /// <summary>灯丝选择</summary>
    SetFilamentSelect = 0x001D,

    /// <summary>放电时间</summary>
    SetDischargeTime = 0x001E,

    /// <summary>保存参数</summary>
    SaveParameters = 0x001A,

    /// <summary>PC模式</summary>
    SetPCMode = 0x001F,

    /// <summary>单机模式</summary>
    SetStandaloneMode = 0x0020,

    /// <summary>PLC模式</summary>
    SetPLCMode = 0x0021,

    /// <summary>错误标记清除</summary>
    ClearErrorFlag = 0x0022,

    /// <summary>输出电流/分压选择</summary>
    SetOutputMode = 0x0023,

    /// <summary>分压模式</summary>
    SetVoltageDivisionMode = 0x0024,

    /// <summary>m/Z设置</summary>
    SetMZ = 0x0025,

    /// <summary>总压读取</summary>
    ReadTotalPressure = 0x0026,

    /// <summary>总流读取</summary>
    ReadTotalFlow = 0x0027,

    /// <summary>开始校验</summary>
    StartCalibration = 0x0028,

    /// <summary>真空系数校正</summary>
    SetVacuumCoefficient = 0x002E,

    /// <summary>设置波特率</summary>
    SetBaudRate = 0x0034
}

/// <summary>
/// EM板子系统功能码
/// </summary>
public enum EMBoardFunctionCode : ushort
{
    /// <summary>设置DET_HV电压</summary>
    SetDET_HVVoltage = 0x000A,

    /// <summary>设置Anode电压</summary>
    SetAnodeVoltage = 0x000B,

    /// <summary>设置Focus电压</summary>
    SetFocusVoltage = 0x000D,

    /// <summary>设置ElectronEnergy</summary>
    SetElectronEnergy = 0x000E,

    /// <summary>灯丝发射使能</summary>
    SetFilamentEmission = 0x0011,

    /// <summary>灯丝电流安全值</summary>
    SetFilamentCurrentSafety = 0x0015
}

/// <summary>
/// FPGA板子系统功能码
/// </summary>
public enum FPGABoardFunctionCode : ushort
{
    /// <summary>检测器OutB</summary>
    SetDetectorOutB = 0x0006,

    /// <summary>OutC (Lower)</summary>
    SetOutCLower = 0x0007,

    /// <summary>OutD (Upper)</summary>
    SetOutDUpper = 0x0008
}

/// <summary>
/// RF相位模式
/// </summary>
public enum RFPhaseMode : ushort
{
    /// <summary>正极性相位</summary>
    Positive = 0x0001,

    /// <summary>负极性相位</summary>
    Negative = 0x0002
}

/// <summary>
/// RF电流档位
/// </summary>
public enum RFCurrentRange : ushort
{
    /// <summary>2mA</summary>
    mA2 = 0x0001,

    /// <summary>5mA</summary>
    mA5 = 0x0002
}

/// <summary>
/// 灯丝选择
/// </summary>
public enum FilamentSelect : ushort
{
    /// <summary>灯丝A</summary>
    FilamentA = 0x0001,

    /// <summary>灯丝B</summary>
    FilamentB = 0x0002
}

/// <summary>
/// 灯丝状态
/// </summary>
public enum FilamentState : ushort
{
    /// <summary>灯丝已关闭</summary>
    Off = 0x0000,

    /// <summary>灯丝已开启</summary>
    On = 0x0001
}

/// <summary>
/// 输出模式
/// </summary>
public enum OutputMode : ushort
{
    /// <summary>电流输出</summary>
    CurrentOutput = 0x0000,

    /// <summary>分压输出 Torr</summary>
    VoltageOutputTorr = 0x0001,

    /// <summary>分压输出 Pa</summary>
    VoltageOutputPa = 0x0002,

    /// <summary>分压输出 mbar</summary>
    VoltageOutputMbar = 0x0003
}

/// <summary>
/// 分压模式
/// </summary>
public enum VoltageDivisionMode : ushort
{
    xPxG = 0x0000,
    xPoneG = 0x0001,
    PxG = 0x0002,
    PoneG = 0x0003
}

/// <summary>
/// 压力单位
/// </summary>
public enum PressureUnit : ushort
{
    /// <summary>Torr</summary>
    Torr = 0x0000,

    /// <summary>Pa</summary>
    Pa = 0x0001,

    /// <summary>mbar</summary>
    Mbar = 0x0002
}

/// <summary>
/// 波特率设置
/// </summary>
public enum BaudRateSetting : ushort
{
    /// <summary>9600</summary>
    Baud9600 = 0x01,

    /// <summary>115200</summary>
    Baud115200 = 0x02,

    /// <summary>921600 (默认)</summary>
    Baud921600 = 0x03
}

/// <summary>
/// 寄存器地址定义 (用于读取命令) - 基于指令集文档
/// </summary>
public static class RGAHoldingRegisters
{
    #region 心跳
    /// <summary>PS板心跳</summary>
    public const ushort PSHeartbeat = 0x000C;
    /// <summary>SYS板心跳</summary>
    public const ushort SYSHeartbeat = 0x000B;
    #endregion

    #region RF相关
    /// <summary>RF反馈1</summary>
    public const ushort RF_Fb1 = 0x0017;
    /// <summary>RF反馈2</summary>
    public const ushort RF_Fb2 = 0x0018;
    /// <summary>RF初级电流</summary>
    public const ushort RFPrimaryCurrent = 0x0019;
    #endregion

    #region 真空度
    /// <summary>真空度</summary>
    public const ushort VacuumDegree = 0x0014;
    #endregion

    #region 电源电压
    /// <summary>Avcc电压(24V)</summary>
    public const ushort AvccVoltage = 0x001E;
    /// <summary>Vcc1电压(5V)</summary>
    public const ushort Vcc1Voltage = 0x0020;
    /// <summary>Vcc2电压(3.3V)</summary>
    public const ushort Vcc2Voltage = 0x0022;
    /// <summary>V+电压(15V)</summary>
    public const ushort VPositiveVoltage = 0x0024;
    /// <summary>V-电压(5V)</summary>
    public const ushort VNegativeVoltage = 0x0026;
    #endregion

    #region 电流
    /// <summary>Avcc电流(24V)</summary>
    public const ushort AvccCurrent = 0x0028;
    #endregion

    #region EM板/检测器相关
    /// <summary>Anode电流</summary>
    public const ushort AnodeCurrent = 0x002A;
    /// <summary>灯丝电流</summary>
    public const ushort FilamentCurrent = 0x002C;
    /// <summary>EM电压</summary>
    public const ushort EMVoltage = 0x002E;
    /// <summary>EM电流</summary>
    public const ushort EMCurrent = 0x003A;
    /// <summary>灯丝电压</summary>
    public const ushort FilamentVoltage = 0x0034;
    /// <summary>Anode驱动电流</summary>
    public const ushort AnodeDriveCurrent = 0x0036;
    /// <summary>Anode电压</summary>
    public const ushort AnodeVoltage = 0x0038;
    /// <summary>灯丝激发电流</summary>
    public const ushort FilamentEmissionCurrent = 0x003C;
    /// <summary>ElectronEnergy电压</summary>
    public const ushort ElectronEnergy = 0x003E;
    /// <summary>ElectronEnergySet MOS管G极</summary>
    public const ushort ElectronEnergySet = 0x0040;
    /// <summary>FocusSet MOS管G极</summary>
    public const ushort FocusSet = 0x0041;
    /// <summary>Focus电压</summary>
    public const ushort FocusVoltage = 0x0041;
    #endregion

    #region 系统
    /// <summary>波特率</summary>
    public const ushort BaudRate = 0x0082;
    #endregion
}
