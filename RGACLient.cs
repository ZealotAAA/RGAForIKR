using System;
using System.Collections.Generic;
using System.Threading;

namespace RGASDK;

/// <summary>
/// RGA高级客户端 - 提供面向业务的API接口
/// </summary>
public class RGACLient : IDisposable
{
    private readonly RGACommunicator _communicator;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// 日志事件
    /// </summary>
    public event EventHandler<string>? LogReceived;

    /// <summary>
    /// 错误事件
    /// </summary>
    public event EventHandler<Exception>? ErrorOccurred;

    /// <summary>
    /// 通信器是否已连接
    /// </summary>
    public bool IsConnected => _communicator.IsConnected;

    /// <summary>
    /// 当前从机地址
    /// </summary>
    public byte SlaveAddress
    {
        get => _communicator.CurrentSlaveAddress;
        set => _communicator.CurrentSlaveAddress = value;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public RGACLient(RGACommunicator communicator)
    {
        _communicator = communicator ?? throw new ArgumentNullException(nameof(communicator));
        _communicator.LogReceived += (s, e) => LogReceived?.Invoke(this, e);
        _communicator.ErrorOccurred += (s, e) => ErrorOccurred?.Invoke(this, e);
    }

    /// <summary>
    /// 构造函数 - 自动创建通信器
    /// </summary>
    public RGACLient(string portName, int baudRate = 921600)
    {
        _communicator = new RGACommunicator();
        _communicator.Connect(portName, baudRate);
        _communicator.LogReceived += (s, e) => LogReceived?.Invoke(this, e);
        _communicator.ErrorOccurred += (s, e) => ErrorOccurred?.Invoke(this, e);
    }

    #region 连接管理

    /// <summary>
    /// 连接到RGA设备
    /// </summary>
    public void Connect(string portName, int baudRate = 921600)
    {
        _communicator.Connect(portName, baudRate);
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public void Disconnect()
    {
        _communicator.Disconnect();
    }

    /// <summary>
    /// 发送原始数据并获取响应
    /// </summary>
    public byte[] SendRaw(byte[] data)
    {
        lock (_lock)
        {
            return _communicator.SendRaw(data);
        }
    }

    #endregion

    #region RF控制

    /// <summary>
    /// 设置RF频率
    /// </summary>
    public RGAOperationResult SetRFFrequency(uint frequencyHz)
    {
        try
        {
            lock (_lock)
            {
                var data = RGACommands.SetRFFrequency(frequencyHz);
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.ControlBoard,
                    (ushort)ControlBoardFunctionCode.SetRFFrequency,
                    data);
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("设置RF频率失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 设置RF中心电压
    /// </summary>
    public RGAOperationResult SetRFCenterVoltage(double voltage)
    {
        try
        {
            lock (_lock)
            {
                var data = RGACommands.SetRFCenterVoltage(voltage);
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.ControlBoard,
                    (ushort)ControlBoardFunctionCode.SetRFCenterVoltage,
                    data);
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("设置RF中心电压失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 禁用RF电压
    /// </summary>
    public RGAOperationResult DisableRFVoltage()
    {
        try
        {
            lock (_lock)
            {
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.ControlBoard,
                    (ushort)ControlBoardFunctionCode.DisableRFVoltage,
                    new ushort[] { 0, 0, 0, 0, 0, 0 });
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("禁用RF电压失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 使能RF电压
    /// </summary>
    public RGAOperationResult EnableRFVoltage()
    {
        try
        {
            lock (_lock)
            {
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.ControlBoard,
                    (ushort)ControlBoardFunctionCode.EnableRFVoltage,
                    new ushort[] { 0, 0, 0, 0, 0, 0 });
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("使能RF电压失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 设置RF相位
    /// </summary>
    public RGAOperationResult SetRFPhase(RFPhaseMode mode)
    {
        try
        {
            lock (_lock)
            {
                var data = new ushort[] { (ushort)mode, 0, 0, 0, 0, 0 };
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.ControlBoard,
                    (ushort)ControlBoardFunctionCode.SetRFPhase,
                    data);
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("设置RF相位失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 设置RF电流档位
    /// </summary>
    public RGAOperationResult SetRFCurrentRange(RFCurrentRange range)
    {
        try
        {
            lock (_lock)
            {
                var data = new ushort[] { (ushort)range, 0, 0, 0, 0, 0 };
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.ControlBoard,
                    (ushort)ControlBoardFunctionCode.SetRF10or5mA,
                    data);
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("设置RF电流档位失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    #endregion

    #region 扫描控制

    /// <summary>
    /// 设置扫描参数
    /// </summary>
    public RGAOperationResult SetScanParameters(ScanParameters scanParams)
    {
        try
        {
            if (!scanParams.IsValid())
                return RGAOperationResult.Fail("扫描参数无效");

            lock (_lock)
            {
                var data = RGACommands.SetScanParameters(scanParams);
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.ControlBoard,
                    (ushort)ControlBoardFunctionCode.SetScanParameters,
                    data);
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("设置扫描参数失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 开始扫描
    /// </summary>
    public RGAOperationResult StartScan(ScanConfig config)
    {
        try
        {
            lock (_lock)
            {
                var data = RGACommands.ScanStart(config);
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.ControlBoard,
                    (ushort)ControlBoardFunctionCode.ScanStart,
                    data);
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("开始扫描失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 停止扫描
    /// </summary>
    public RGAOperationResult StopScan()
    {
        try
        {
            lock (_lock)
            {
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.ControlBoard,
                    (ushort)ControlBoardFunctionCode.ScanStop,
                    new ushort[] { 0, 0, 0, 0, 0, 0 });
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("停止扫描失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    #endregion

    #region 灯丝控制

    /// <summary>
    /// 选择灯丝
    /// </summary>
    public RGAOperationResult SelectFilament(FilamentSelect filament)
    {
        try
        {
            lock (_lock)
            {
                var data = new ushort[] { (ushort)filament, 0, 0, 0, 0, 0 };
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.ControlBoard,
                    (ushort)ControlBoardFunctionCode.SetFilamentSelect,
                    data);
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("选择灯丝失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 设置灯丝发射电流
    /// </summary>
    public RGAOperationResult SetFilamentEmission(ushort currentUA)
    {
        try
        {
            lock (_lock)
            {
                var data = RGACommands.SetFilamentEmission(currentUA);
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.EMBoard,
                    (ushort)EMBoardFunctionCode.SetFilamentEmission,
                    data);
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("设置灯丝发射电流失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 设置放电时间
    /// </summary>
    public RGAOperationResult SetDischargeTime(ushort timeUs)
    {
        try
        {
            lock (_lock)
            {
                var data = RGACommands.SetDischargeTime(timeUs);
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.ControlBoard,
                    (ushort)ControlBoardFunctionCode.SetDischargeTime,
                    data);
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("设置放电时间失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 设置灯丝电流安全值
    /// </summary>
    public RGAOperationResult SetFilamentCurrentSafety(double currentA)
    {
        try
        {
            lock (_lock)
            {
                var data = RGACommands.SetFilamentCurrentSafety(currentA);
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.EMBoard,
                    (ushort)EMBoardFunctionCode.SetFilamentCurrentSafety,
                    data);
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("设置灯丝电流安全值失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    #endregion

    #region EM板(检测器)控制

    /// <summary>
    /// 设置DET_HV电压
    /// </summary>
    public RGAOperationResult SetDET_HVVoltage(ushort voltage)
    {
        try
        {
            lock (_lock)
            {
                var data = RGACommands.SetDET_HVVoltage(voltage);
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.EMBoard,
                    (ushort)EMBoardFunctionCode.SetDET_HVVoltage,
                    data);
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("设置DET_HV电压失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 设置Anode电压
    /// </summary>
    public RGAOperationResult SetAnodeVoltage(ushort voltage)
    {
        try
        {
            lock (_lock)
            {
                var data = RGACommands.SetAnodeVoltage(voltage);
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.EMBoard,
                    (ushort)EMBoardFunctionCode.SetAnodeVoltage,
                    data);
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("设置Anode电压失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 设置Focus电压
    /// </summary>
    public RGAOperationResult SetFocusVoltage(ushort voltage)
    {
        try
        {
            lock (_lock)
            {
                var data = RGACommands.SetFocusVoltage(voltage);
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.EMBoard,
                    (ushort)EMBoardFunctionCode.SetFocusVoltage,
                    data);
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("设置Focus电压失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 设置ElectronEnergy
    /// </summary>
    public RGAOperationResult SetElectronEnergy(ushort energy)
    {
        try
        {
            lock (_lock)
            {
                var data = RGACommands.SetElectronEnergy(energy);
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.EMBoard,
                    (ushort)EMBoardFunctionCode.SetElectronEnergy,
                    data);
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("设置ElectronEnergy失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    #endregion

    #region FPGA检测器控制

    /// <summary>
    /// 设置检测器OutB
    /// </summary>
    public RGAOperationResult SetDetectorOutB(ushort value)
    {
        try
        {
            lock (_lock)
            {
                var data = RGACommands.SetDetectorOutB(value);
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.FPGABoard,
                    (ushort)FPGABoardFunctionCode.SetDetectorOutB,
                    data);
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("设置检测器OutB失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 设置OutC (Lower)
    /// </summary>
    public RGAOperationResult SetOutCLower(ushort value)
    {
        try
        {
            lock (_lock)
            {
                var data = RGACommands.SetOutCLower(value);
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.FPGABoard,
                    (ushort)FPGABoardFunctionCode.SetOutCLower,
                    data);
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("设置OutC Lower失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 设置OutD (Upper)
    /// </summary>
    public RGAOperationResult SetOutDUpper(ushort value)
    {
        try
        {
            lock (_lock)
            {
                var data = RGACommands.SetOutDUpper(value);
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.FPGABoard,
                    (ushort)FPGABoardFunctionCode.SetOutDUpper,
                    data);
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("设置OutD Upper失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    #endregion

    #region 系统控制

    /// <summary>
    /// 保存参数
    /// </summary>
    public RGAOperationResult SaveParameters()
    {
        try
        {
            lock (_lock)
            {
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.ControlBoard,
                    (ushort)ControlBoardFunctionCode.SaveParameters,
                    new ushort[] { 0, 0, 0, 0, 0, 0 });
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("保存参数失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 设置真空系数校正
    /// </summary>
    public RGAOperationResult SetVacuumCoefficient(double coefficient)
    {
        try
        {
            lock (_lock)
            {
                var data = RGACommands.SetVacuumCoefficient(coefficient);
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.ControlBoard,
                    (ushort)ControlBoardFunctionCode.SetVacuumCoefficient,
                    data);
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("设置真空系数校正失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 设置波特率
    /// </summary>
    public RGAOperationResult SetBaudRate(BaudRateSetting baudRate)
    {
        try
        {
            lock (_lock)
            {
                var data = new ushort[] { (ushort)baudRate, 0, 0, 0, 0, 0 };
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.ControlBoard,
                    (ushort)ControlBoardFunctionCode.SetBaudRate,
                    data);
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("设置波特率失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 清除错误标记
    /// </summary>
    public RGAOperationResult ClearErrorFlag()
    {
        try
        {
            lock (_lock)
            {
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.ControlBoard,
                    (ushort)ControlBoardFunctionCode.ClearErrorFlag,
                    new ushort[] { 0, 0, 0, 0, 0, 0 });
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("清除错误标记失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 开始校验
    /// </summary>
    public RGAOperationResult StartCalibration()
    {
        try
        {
            lock (_lock)
            {
                bool success = _communicator.SendWriteCommand(
                    (ushort)RGASubsystemAddress.ControlBoard,
                    (ushort)ControlBoardFunctionCode.StartCalibration,
                    new ushort[] { 0, 0, 0, 0, 0, 0 });
                return success ? RGAOperationResult.Ok() : RGAOperationResult.Fail("开始校验失败");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail(ex.Message);
        }
    }

    #endregion

    #region 读取操作

    /// <summary>
    /// 读取PS板心跳
    /// </summary>
    public RGAReadResult<ushort> ReadPSHeartbeat()
    {
        try
        {
            lock (_lock)
            {
                var values = _communicator.SendReadCommand(RGAHoldingRegisters.PSHeartbeat, 1);
                return values.Length > 0
                    ? RGAReadResult<ushort>.Ok(values[0])
                    : RGAReadResult<ushort>.Fail("读取数据为空");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAReadResult<ushort>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 读取SYS板心跳
    /// </summary>
    public RGAReadResult<ushort> ReadSYSHeartbeat()
    {
        try
        {
            lock (_lock)
            {
                var values = _communicator.SendReadCommand(RGAHoldingRegisters.SYSHeartbeat, 1);
                return values.Length > 0
                    ? RGAReadResult<ushort>.Ok(values[0])
                    : RGAReadResult<ushort>.Fail("读取数据为空");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAReadResult<ushort>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 读取真空度
    /// </summary>
    public RGAReadResult<double> ReadVacuumDegree()
    {
        try
        {
            lock (_lock)
            {
                var values = _communicator.SendReadCommand(RGAHoldingRegisters.VacuumDegree, 2);
                if (values.Length >= 2)
                {
                    double vacuum = RGACommands.ConvertVacuumDegree(values[0], values[1]);
                    return RGAReadResult<double>.Ok(vacuum);
                }
                return RGAReadResult<double>.Fail("读取数据不完整");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAReadResult<double>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 读取Anode电压
    /// </summary>
    public RGAReadResult<double> ReadAnodeVoltage()
    {
        try
        {
            lock (_lock)
            {
                var values = _communicator.SendReadCommand(RGAHoldingRegisters.AnodeVoltage, 2);
                if (values.Length >= 2)
                {
                    double voltage = RGACommands.ConvertVoltage(values[0], values[1]); // values[0]=寄存器0, values[1]=寄存器1
                    return RGAReadResult<double>.Ok(voltage);
                }
                return RGAReadResult<double>.Fail("读取数据不完整");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAReadResult<double>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 读取EM电压
    /// </summary>
    public RGAReadResult<double> ReadEMVoltage()
    {
        try
        {
            lock (_lock)
            {
                var values = _communicator.SendReadCommand(RGAHoldingRegisters.EMVoltage, 2);
                if (values.Length >= 2)
                {
                    double voltage = RGACommands.ConvertVoltage(values[0], values[1]); // values[0]=寄存器0, values[1]=寄存器1
                    return RGAReadResult<double>.Ok(voltage);
                }
                return RGAReadResult<double>.Fail("读取数据不完整");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAReadResult<double>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 读取灯丝电流
    /// </summary>
    public RGAReadResult<double> ReadFilamentCurrent()
    {
        try
        {
            lock (_lock)
            {
                var values = _communicator.SendReadCommand(RGAHoldingRegisters.FilamentCurrent, 2);
                if (values.Length >= 2)
                {
                    double current = RGACommands.ConvertFilamentCurrent(values[0], values[1]);
                    return RGAReadResult<double>.Ok(current);
                }
                return RGAReadResult<double>.Fail("读取数据不完整");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAReadResult<double>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 读取ElectronEnergy
    /// </summary>
    public RGAReadResult<double> ReadElectronEnergy()
    {
        try
        {
            lock (_lock)
            {
                var values = _communicator.SendReadCommand(RGAHoldingRegisters.ElectronEnergy, 1);
                if (values.Length >= 1)
                {
                    double energy = RGACommands.ConvertElectronEnergyDisplay(values[0]);
                    return RGAReadResult<double>.Ok(energy);
                }
                return RGAReadResult<double>.Fail("读取数据为空");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAReadResult<double>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 读取Focus电压
    /// </summary>
    public RGAReadResult<ushort> ReadFocusVoltage()
    {
        try
        {
            lock (_lock)
            {
                var values = _communicator.SendReadCommand(RGAHoldingRegisters.FocusVoltage, 1);
                return values.Length > 0
                    ? RGAReadResult<ushort>.Ok(values[0])
                    : RGAReadResult<ushort>.Fail("读取数据为空");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAReadResult<ushort>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 读取RF频率反馈
    /// </summary>
    public RGAReadResult<uint> ReadRFFrequency()
    {
        try
        {
            lock (_lock)
            {
                var values = _communicator.SendReadCommand(RGAHoldingRegisters.RF_Fb1, 2);
                if (values.Length >= 2)
                {
                    uint frequency = RGACommands.ReadRFFrequency(values[0], values[1]);
                    return RGAReadResult<uint>.Ok(frequency);
                }
                return RGAReadResult<uint>.Fail("读取数据不完整");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAReadResult<uint>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 读取灯丝激发电流
    /// </summary>
    public RGAReadResult<ushort> ReadFilamentEmissionCurrent()
    {
        try
        {
            lock (_lock)
            {
                var values = _communicator.SendReadCommand(RGAHoldingRegisters.FilamentEmissionCurrent, 2);
                return values.Length > 0
                    ? RGAReadResult<ushort>.Ok(values[0])
                    : RGAReadResult<ushort>.Fail("读取数据为空");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAReadResult<ushort>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// 读取设备完整状态
    /// </summary>
    public RGAReadResult<RGAStatusInfo> ReadStatus()
    {
        try
        {
            var status = new RGAStatusInfo();

            lock (_lock)
            {
                var psHeartbeat = _communicator.SendReadCommand(RGAHoldingRegisters.PSHeartbeat, 1);
                if (psHeartbeat.Length > 0) status.PSHeartbeat = psHeartbeat[0];

                var sysHeartbeat = _communicator.SendReadCommand(RGAHoldingRegisters.SYSHeartbeat, 1);
                if (sysHeartbeat.Length > 0) status.SYSHeartbeat = sysHeartbeat[0];

                var focusVoltage = _communicator.SendReadCommand(RGAHoldingRegisters.FocusVoltage, 1);
                if (focusVoltage.Length > 0) status.FocusVoltage = focusVoltage[0];

                var electronEnergy = _communicator.SendReadCommand(RGAHoldingRegisters.ElectronEnergy, 1);
                if (electronEnergy.Length > 0) status.ElectronEnergy = RGACommands.ConvertElectronEnergyDisplay(electronEnergy[0]);

                var rfPrimaryCurrent = _communicator.SendReadCommand(RGAHoldingRegisters.RFPrimaryCurrent, 1);
                if (rfPrimaryCurrent.Length > 0) status.RFPrimaryCurrent = rfPrimaryCurrent[0];

                var rfFb1 = _communicator.SendReadCommand(RGAHoldingRegisters.RF_Fb1, 1);
                if (rfFb1.Length > 0) status.RF_Fb1 = rfFb1[0];

                var rfFb2 = _communicator.SendReadCommand(RGAHoldingRegisters.RF_Fb2, 1);
                if (rfFb2.Length > 0) status.RF_Fb2 = rfFb2[0];

                var vacuumDegree = _communicator.SendReadCommand(RGAHoldingRegisters.VacuumDegree, 2);
                if (vacuumDegree.Length >= 2) status.VacuumDegree = RGACommands.ConvertVacuumDegree(vacuumDegree[0], vacuumDegree[1]);

                var anodeVoltage = _communicator.SendReadCommand(RGAHoldingRegisters.AnodeVoltage, 2);
                if (anodeVoltage.Length >= 2) status.AnodeVoltage = RGACommands.ConvertVoltage(anodeVoltage[0], anodeVoltage[1]);

                var emVoltage = _communicator.SendReadCommand(RGAHoldingRegisters.EMVoltage, 2);
                if (emVoltage.Length >= 2) status.EMVoltage = RGACommands.ConvertVoltage(emVoltage[0], emVoltage[1]);

                var filamentCurrent = _communicator.SendReadCommand(RGAHoldingRegisters.FilamentCurrent, 2);
                if (filamentCurrent.Length >= 2) status.FilamentCurrent = RGACommands.ConvertFilamentCurrent(filamentCurrent[0], filamentCurrent[1]);

                var anodeCurrent = _communicator.SendReadCommand(RGAHoldingRegisters.AnodeCurrent, 2);
                if (anodeCurrent.Length >= 2) status.AnodeCurrent = RGACommands.ConvertCurrent(anodeCurrent[0], anodeCurrent[1]);

                var emCurrent = _communicator.SendReadCommand(RGAHoldingRegisters.EMCurrent, 2);
                if (emCurrent.Length >= 2) status.EMCurrent = RGACommands.ConvertCurrent(emCurrent[0], emCurrent[1]);

                var filamentVoltage = _communicator.SendReadCommand(RGAHoldingRegisters.FilamentVoltage, 2);
                if (filamentVoltage.Length >= 2) status.FilamentVoltage = RGACommands.ConvertVoltage(filamentVoltage[0], filamentVoltage[1]);

                var avccVoltage = _communicator.SendReadCommand(RGAHoldingRegisters.AvccVoltage, 2);
                if (avccVoltage.Length >= 2) status.AvccVoltage = RGACommands.ConvertVoltage(avccVoltage[0], avccVoltage[1]);

                var vcc1Voltage = _communicator.SendReadCommand(RGAHoldingRegisters.Vcc1Voltage, 2);
                if (vcc1Voltage.Length >= 2) status.Vcc1Voltage = RGACommands.ConvertVoltage(vcc1Voltage[0], vcc1Voltage[1]);

                var vcc2Voltage = _communicator.SendReadCommand(RGAHoldingRegisters.Vcc2Voltage, 2);
                if (vcc2Voltage.Length >= 2) status.Vcc2Voltage = RGACommands.ConvertVoltage(vcc2Voltage[0], vcc2Voltage[1]);

                var vPositiveVoltage = _communicator.SendReadCommand(RGAHoldingRegisters.VPositiveVoltage, 2);
                if (vPositiveVoltage.Length >= 2) status.VPositiveVoltage = RGACommands.ConvertVoltage(vPositiveVoltage[0], vPositiveVoltage[1]);

                var vNegativeVoltage = _communicator.SendReadCommand(RGAHoldingRegisters.VNegativeVoltage, 2);
                if (vNegativeVoltage.Length >= 2) status.VNegativeVoltage = RGACommands.ConvertVoltage(vNegativeVoltage[0], vNegativeVoltage[1]);

                var avccCurrent = _communicator.SendReadCommand(RGAHoldingRegisters.AvccCurrent, 2);
                if (avccCurrent.Length >= 2) status.AvccCurrent = RGACommands.ConvertCurrent(avccCurrent[0], avccCurrent[1]);
            }

            return RGAReadResult<RGAStatusInfo>.Ok(status);
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAReadResult<RGAStatusInfo>.Fail(ex.Message);
        }
    }

    #endregion

    #region m/z 扫描数据读取

    /// <summary>
    /// 读取质量数范围的波形数据
    /// </summary>
    public RGAReadResult<List<MZScanData>> ReadMassScanData(double startMass, double endMass, double stepSize = 0.1)
    {
        try
        {
            ushort baseRegisterAddress = 4200;
            int pointCount = (int)((endMass - startMass) / stepSize) + 1;

            if (pointCount <= 0 || pointCount > 1000)
                return RGAReadResult<List<MZScanData>>.Fail("扫描点数量无效");

            ushort startAddress = (ushort)(baseRegisterAddress + (int)(startMass * 10));

            lock (_lock)
            {
                var values = _communicator.SendReadCommand(startAddress, (ushort)pointCount);

                var scanData = new List<MZScanData>();
                for (int i = 0; i < values.Length; i++)
                {
                    double mass = startMass + (i * stepSize);
                    scanData.Add(new MZScanData
                    {
                        MassNumber = mass,
                        PeakHeight = values[i]
                    });
                }

                return RGAReadResult<List<MZScanData>>.Ok(scanData);
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAReadResult<List<MZScanData>>.Fail(ex.Message);
        }
    }

    #endregion

    #region 设备初始化流程

    /// <summary>
    /// 执行完整的设备初始化
    /// </summary>
    public RGAOperationResult InitializeDevice(EMBoardConfig emConfig, RFConfig rfConfig, FilamentConfig filamentConfig)
    {
        try
        {
            var result = SelectFilament(filamentConfig.Filament);
            if (!result.Success) return result;

            result = SetDET_HVVoltage(emConfig.DET_HVVoltage);
            if (!result.Success) return result;

            result = SetAnodeVoltage(emConfig.AnodeVoltage);
            if (!result.Success) return result;

            result = SetFocusVoltage(emConfig.FocusVoltage);
            if (!result.Success) return result;

            result = SetElectronEnergy(emConfig.ElectronEnergy);
            if (!result.Success) return result;

            result = SetRFFrequency(rfConfig.Frequency);
            if (!result.Success) return result;

            result = SetRFCenterVoltage(rfConfig.CenterVoltage);
            if (!result.Success) return result;

            result = SetRFPhase(rfConfig.PhaseMode);
            if (!result.Success) return result;

            result = SetRFCurrentRange(rfConfig.CurrentRange);
            if (!result.Success) return result;

            result = SetFilamentEmission(filamentConfig.EmissionCurrent);
            if (!result.Success) return result;

            result = SetDischargeTime(filamentConfig.DischargeTime);
            if (!result.Success) return result;

            result = SaveParameters();
            if (!result.Success) return result;

            return RGAOperationResult.Ok();
        }
        catch (Exception ex)
        {
            OnErrorOccurred(ex);
            return RGAOperationResult.Fail($"设备初始化失败: {ex.Message}");
        }
    }

    #endregion

    #region 事件

    protected virtual void OnLogReceived(string message)
    {
        LogReceived?.Invoke(this, message);
    }

    protected virtual void OnErrorOccurred(Exception ex)
    {
        ErrorOccurred?.Invoke(this, ex);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (!_disposed)
        {
            _communicator.Dispose();
            _disposed = true;
        }
    }

    #endregion
}
