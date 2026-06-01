using System.IO.Ports;

namespace RGASDK;

/// <summary>
/// RGA Modbus RTU 协议处理器
/// 负责构建和解析Modbus消息
/// </summary>
public class RGAProtocolHandler
{
    private const byte ModbusFunctionWriteMultiple = 0x10;
    private const byte ModbusFunctionReadHolding = 0x03;
    private const ushort BaseRegisterAddress = 0x0000;

    /// <summary>
    /// 计算CRC16校验码 (Modbus标准)
    /// </summary>
    public static ushort CalculateCRC16(byte[] data)
    {
        ushort crc = 0xFFFF;
        foreach (byte b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x0001) != 0)
                {
                    crc >>= 1;
                    crc ^= 0xA001;
                }
                else
                {
                    crc >>= 1;
                }
            }
        }
        return crc;
    }

    /// <summary>
    /// 构建写命令帧
    /// </summary>
    /// <param name="slaveAddress">从机地址 (1-255)</param>
    /// <param name="subsystemAddress">子系统地址</param>
    /// <param name="subsystemFunctionCode">子系统功能码</param>
    /// <param name="data">数据数组 (最多6个16位值)</param>
    /// <returns>完整的Modbus帧(含CRC)</returns>
    public byte[] BuildWriteCommand(byte slaveAddress, ushort subsystemAddress, ushort subsystemFunctionCode, ushort[] data)
    {
        if (data == null || data.Length > 6)
            throw new ArgumentException("数据数组长度必须在1-6之间");

        const int registerCount = 10;
        int byteCount = registerCount * 2;

        var frame = new List<byte>
        {
            slaveAddress,
            ModbusFunctionWriteMultiple,
            (byte)(BaseRegisterAddress >> 8),
            (byte)(BaseRegisterAddress & 0xFF),
            (byte)(registerCount >> 8),
            (byte)(registerCount & 0xFF),
            (byte)byteCount,
            (byte)(subsystemAddress >> 8),
            (byte)(subsystemAddress & 0xFF),
            (byte)(subsystemFunctionCode >> 8),
            (byte)(subsystemFunctionCode & 0xFF),
            0x00, 0x01
        };

        for (int i = 0; i < 6; i++)
        {
            if (i < data.Length)
            {
                frame.Add((byte)(data[i] >> 8));
                frame.Add((byte)(data[i] & 0xFF));
            }
            else
            {
                frame.Add(0x00);
                frame.Add(0x00);
            }
        }

        var crc = CalculateCRC16(frame.ToArray());
        frame.Add((byte)(crc & 0xFF));
        frame.Add((byte)(crc >> 8));

        return frame.ToArray();
    }

    /// <summary>
    /// 构建读寄存器帧
    /// </summary>
    public byte[] BuildReadCommand(byte slaveAddress, ushort startAddress, ushort quantity)
    {
        var frame = new List<byte>
        {
            slaveAddress,
            ModbusFunctionReadHolding,
            (byte)(startAddress >> 8),
            (byte)(startAddress & 0xFF),
            (byte)(quantity >> 8),
            (byte)(quantity & 0xFF)
        };

        var crc = CalculateCRC16(frame.ToArray());
        frame.Add((byte)(crc & 0xFF));
        frame.Add((byte)(crc >> 8));

        return frame.ToArray();
    }

    /// <summary>
    /// 解析读命令返回数据
    /// </summary>
    public ushort[] ParseReadResponse(byte[] response)
    {
        if (response == null || response.Length < 5)
            throw new ArgumentException("响应数据长度无效");

        if (response[1] != ModbusFunctionReadHolding)
            throw new InvalidOperationException("功能码不匹配");

        byte byteCount = response[2];
        var values = new List<ushort>();

        for (int i = 0; i < byteCount; i += 2)
        {
            ushort value = (ushort)((response[3 + i] << 8) | response[4 + i]);
            values.Add(value);
        }

        return values.ToArray();
    }

    /// <summary>
    /// 验证CRC校验
    /// </summary>
    public bool VerifyCRC(byte[] data)
    {
        if (data == null || data.Length < 3)
            return false;

        int length = data.Length - 2;
        var payload = data.Take(length).ToArray();
        var receivedCRC = (ushort)((data[data.Length - 1] << 8) | data[data.Length - 2]);
        var calculatedCRC = CalculateCRC16(payload);

        return receivedCRC == calculatedCRC;
    }

    /// <summary>
    /// 将字节数组转换为十六进制字符串
    /// </summary>
    public static string ToHexString(byte[] data)
    {
        return BitConverter.ToString(data).Replace("-", " ");
    }

    #region 读取命令 - 根据指令集文档

    /// <summary>
    /// 读取PS板心跳
    /// 寄存器地址: 0x000C, 数量: 1
    /// </summary>
    public byte[] BuildReadPSHeartbeat(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x000C, 1);
    }

    /// <summary>
    /// 读取SYS板心跳
    /// 寄存器地址: 0x000B, 数量: 1
    /// </summary>
    public byte[] BuildReadSYSHeartbeat(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x000B, 1);
    }

    /// <summary>
    /// 读取Anode电压
    /// 寄存器地址: 0x0038, 数量: 2
    /// </summary>
    public byte[] BuildReadAnodeVoltage(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x0038, 2);
    }

    /// <summary>
    /// 读取EM电压
    /// 寄存器地址: 0x002E, 数量: 2
    /// </summary>
    public byte[] BuildReadEMVoltage(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x002E, 2);
    }

    /// <summary>
    /// 读取灯丝激发电流
    /// 寄存器地址: 0x003C, 数量: 2
    /// </summary>
    public byte[] BuildReadFilamentEmissionCurrent(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x003C, 2);
    }

    /// <summary>
    /// 读取Focus电压
    /// 寄存器地址: 0x0041, 数量: 1
    /// </summary>
    public byte[] BuildReadFocusVoltage(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x0041, 1);
    }

    /// <summary>
    /// 读取ElectronEnergy电压(Vin0)
    /// 寄存器地址: 0x003E, 数量: 1
    /// 显示值 = 读取值 * 5 / 64.8
    /// </summary>
    public byte[] BuildReadElectronEnergy(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x003E, 1);
    }

    /// <summary>
    /// 读取灯丝电压
    /// 寄存器地址: 0x0034, 数量: 2
    /// </summary>
    public byte[] BuildReadFilamentVoltage(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x0034, 2);
    }

    /// <summary>
    /// 读取灯丝电流
    /// 寄存器地址: 0x002C, 数量: 2
    /// </summary>
    public byte[] BuildReadFilamentCurrent(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x002C, 2);
    }

    /// <summary>
    /// 读取Anode电流
    /// 寄存器地址: 0x002A, 数量: 2
    /// </summary>
    public byte[] BuildReadAnodeCurrent(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x002A, 2);
    }

    /// <summary>
    /// 读取Anode驱动电流
    /// 寄存器地址: 0x0036, 数量: 2
    /// </summary>
    public byte[] BuildReadAnodeDriveCurrent(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x0036, 2);
    }

    /// <summary>
    /// 读取EM电流
    /// 寄存器地址: 0x003A, 数量: 2
    /// </summary>
    public byte[] BuildReadEMCurrent(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x003A, 2);
    }

    /// <summary>
    /// 读取ElectronEnergySet MOS管G极(Vin2)
    /// 寄存器地址: 0x0040, 数量: 1
    /// </summary>
    public byte[] BuildReadElectronEnergySet(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x0040, 1);
    }

    /// <summary>
    /// 读取RF初级电流
    /// 寄存器地址: 0x0019, 数量: 1
    /// </summary>
    public byte[] BuildReadRFPrimaryCurrent(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x0019, 1);
    }

    /// <summary>
    /// 读取RF反馈1
    /// 寄存器地址: 0x0017, 数量: 1
    /// </summary>
    public byte[] BuildReadRF_Fb1(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x0017, 1);
    }

    /// <summary>
    /// 读取RF反馈2
    /// 寄存器地址: 0x0018, 数量: 1
    /// </summary>
    public byte[] BuildReadRF_Fb2(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x0018, 1);
    }

    /// <summary>
    /// 读取真空度
    /// 寄存器地址: 0x0014, 数量: 2
    /// </summary>
    public byte[] BuildReadVacuumDegree(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x0014, 2);
    }

    /// <summary>
    /// 读取Avcc电压(24V)
    /// 寄存器地址: 0x001E, 数量: 2
    /// </summary>
    public byte[] BuildReadAvccVoltage(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x001E, 2);
    }

    /// <summary>
    /// 读取Vcc2电压(3.3V)
    /// 寄存器地址: 0x0022, 数量: 2
    /// </summary>
    public byte[] BuildReadVcc2Voltage(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x0022, 2);
    }

    /// <summary>
    /// 读取Vcc1电压(5V)
    /// 寄存器地址: 0x0020, 数量: 2
    /// </summary>
    public byte[] BuildReadVcc1Voltage(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x0020, 2);
    }

    /// <summary>
    /// 读取V-电压(5V)
    /// 寄存器地址: 0x0026, 数量: 2
    /// </summary>
    public byte[] BuildReadVNegativeVoltage(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x0026, 2);
    }

    /// <summary>
    /// 读取V+电压(15V)
    /// 寄存器地址: 0x0024, 数量: 2
    /// </summary>
    public byte[] BuildReadVPositiveVoltage(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x0024, 2);
    }

    /// <summary>
    /// 读取Avcc电流(24V)
    /// 寄存器地址: 0x0028, 数量: 2
    /// </summary>
    public byte[] BuildReadAvccCurrent(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x0028, 2);
    }

    /// <summary>
    /// 读取波特率
    /// 寄存器地址: 0x0082, 数量: 2
    /// </summary>
    public byte[] BuildReadBaudRate(byte slaveAddress = 0x01)
    {
        return BuildReadCommand(slaveAddress, 0x0082, 2);
    }

    /// <summary>
    /// 读取质量数波形数据
    /// 基础地址: 4200, 示例: 读取质量数27.5~28.5, 起始地址=4200+275=4475, 数量=10
    /// </summary>
    public byte[] BuildReadMassScanData(byte slaveAddress, ushort startMassNumber, ushort pointCount)
    {
        ushort baseAddress = 4200;
        ushort startAddress = (ushort)(baseAddress + (startMassNumber * 10));
        return BuildReadCommand(slaveAddress, startAddress, pointCount);
    }

    #endregion
}
