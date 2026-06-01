using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace RGASDK;

/// <summary>
/// RGA通信器 - 负责串口通信和命令发送
/// </summary>
public class RGACommunicator : IDisposable
{
    private SerialPort? _serialPort;
    private readonly RGAProtocolHandler _protocol;
    private readonly object _lockObject = new();
    private bool _disposed;
    private int _readTimeout = 1000;
    private int _writeTimeout = 1000;
    private int _retryCount = 3;
    private int _retryDelay = 100;

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
    public bool IsConnected => _serialPort?.IsOpen ?? false;

    /// <summary>
    /// 当前从机地址
    /// </summary>
    public byte CurrentSlaveAddress { get; set; } = 1;

    /// <summary>
    /// 读取超时时间 (毫秒)
    /// </summary>
    public int ReadTimeout
    {
        get => _readTimeout;
        set => _readTimeout = Math.Max(100, value);
    }

    /// <summary>
    /// 写入超时时间 (毫秒)
    /// </summary>
    public int WriteTimeout
    {
        get => _writeTimeout;
        set => _writeTimeout = Math.Max(100, value);
    }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount
    {
        get => _retryCount;
        set => _retryCount = Math.Max(0, Math.Min(10, value));
    }

    /// <summary>
    /// 重试间隔时间 (毫秒)
    /// </summary>
    public int RetryDelay
    {
        get => _retryDelay;
        set => _retryDelay = Math.Max(10, value);
    }

    /// <summary>
    /// 获取协议处理器
    /// </summary>
    public RGAProtocolHandler Protocol => _protocol;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RGACommunicator()
    {
        _protocol = new RGAProtocolHandler();
    }

    /// <summary>
    /// 连接到RGA设备
    /// </summary>
    public void Connect(string portName, int baudRate = 921600)
    {
        lock (_lockObject)
        {
            Disconnect();

            _serialPort = new SerialPort(portName, baudRate)
            {
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                ReadTimeout = _readTimeout,
                WriteTimeout = _writeTimeout
            };

            _serialPort.Open();
            OnLogReceived($"已连接到 {portName} @ {baudRate}bps");
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public void Disconnect()
    {
        lock (_lockObject)
        {
            if (_serialPort != null)
            {
                try
                {
                    if (_serialPort.IsOpen)
                        _serialPort.Close();
                    _serialPort.Dispose();
                }
                catch { }
                _serialPort = null;
                OnLogReceived("已断开连接");
            }
        }
    }

    /// <summary>
    /// 获取可用串口列表
    /// </summary>
    public static string[] GetAvailablePorts()
    {
        return SerialPort.GetPortNames();
    }

    #region 同步发送方法

    /// <summary>
    /// 发送写命令并等待响应
    /// </summary>
    public bool SendWriteCommand(ushort subsystemAddress, ushort subsystemFunctionCode, ushort[] data)
    {
        var command = _protocol.BuildWriteCommand(CurrentSlaveAddress, subsystemAddress, subsystemFunctionCode, data);
        OnLogReceived($"发送: {RGAProtocolHandler.ToHexString(command)}");

        return SendAndVerify(command);
    }

    /// <summary>
    /// 发送读命令
    /// </summary>
    public ushort[] SendReadCommand(ushort startAddress, ushort quantity)
    {
        var command = _protocol.BuildReadCommand(CurrentSlaveAddress, startAddress, quantity);
        OnLogReceived($"发送: {RGAProtocolHandler.ToHexString(command)}");

        lock (_lockObject)
        {
            _serialPort!.Write(command, 0, command.Length);

            var response = ReadResponse();
            if (response != null && response.Length > 0)
            {
                OnLogReceived($"接收: {RGAProtocolHandler.ToHexString(response)}");

                if (!_protocol.VerifyCRC(response))
                {
                    OnLogReceived($"CRC校验失败: 期望={RGAProtocolHandler.CalculateCRC16(response.Take(response.Length - 2).ToArray()):X4}, 收到={response[response.Length - 2]:X2}{response[response.Length - 1]:X2}");
                    throw new InvalidOperationException("CRC校验失败");
                }

                return _protocol.ParseReadResponse(response);
            }

            OnLogReceived("读取响应超时: 未收到任何数据");
            throw new TimeoutException("读取响应超时");
        }
    }

    /// <summary>
    /// 发送原始数据
    /// </summary>
    public byte[] SendRaw(byte[] data)
    {
        OnLogReceived($"发送: {RGAProtocolHandler.ToHexString(data)}");

        lock (_lockObject)
        {
            _serialPort!.DiscardInBuffer();
            _serialPort.Write(data, 0, data.Length);

            var response = ReadResponse();
            if (response != null && response.Length > 0)
            {
                OnLogReceived($"接收: {RGAProtocolHandler.ToHexString(response)}");
                return response;
            }

            throw new TimeoutException("读取响应超时");
        }
    }

    private bool SendAndVerify(byte[] command)
    {
        lock (_lockObject)
        {
            for (int retry = 0; retry <= _retryCount; retry++)
            {
                try
                {
                    _serialPort!.DiscardInBuffer();
                    _serialPort.Write(command, 0, command.Length);

                    var response = ReadResponse();
                    if (response != null && response.Length > 0)
                    {
                        OnLogReceived($"接收: {RGAProtocolHandler.ToHexString(response)}");

                        if (_protocol.VerifyCRC(response))
                            return true;
                        else if (retry < _retryCount)
                            Thread.Sleep(_retryDelay);
                    }
                    else if (retry < _retryCount)
                    {
                        Thread.Sleep(_retryDelay);
                    }
                }
                catch (Exception)
                {
                    if (retry >= _retryCount)
                        throw;
                    Thread.Sleep(_retryDelay);
                }
            }

            return false;
        }
    }

    private byte[]? ReadResponse()
    {
        try
        {
            _serialPort!.ReadTimeout = 500;
            byte[] buffer = new byte[256];
            int bytesRead = 0;
            int startTime = Environment.TickCount;
            int maxWaitTime = 500;

            while (bytesRead < buffer.Length)
            {
                if (Environment.TickCount - startTime > maxWaitTime)
                    break;

                if (_serialPort.BytesToRead > 0)
                {
                    int read = _serialPort.Read(buffer, bytesRead, buffer.Length - bytesRead);
                    if (read > 0)
                    {
                        bytesRead += read;
                        startTime = Environment.TickCount;
                    }
                }
                Thread.Sleep(1);
            }

            if (bytesRead > 0)
            {
                var response = new byte[bytesRead];
                Array.Copy(buffer, response, bytesRead);
                return response;
            }

            return null;
        }
        catch (TimeoutException)
        {
            int available = _serialPort!.BytesToRead;
            if (available > 0)
            {
                var response = new byte[available];
                _serialPort.Read(response, 0, available);
                return response;
            }
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    #endregion

    #region 异步发送方法

    /// <summary>
    /// 异步发送写命令
    /// </summary>
    public async Task<bool> SendWriteCommandAsync(ushort subsystemAddress, ushort subsystemFunctionCode, ushort[] data, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var command = _protocol.BuildWriteCommand(CurrentSlaveAddress, subsystemAddress, subsystemFunctionCode, data);
            OnLogReceived($"发送: {RGAProtocolHandler.ToHexString(command)}");
            return SendAndVerify(command);
        }, cancellationToken);
    }

    /// <summary>
    /// 异步发送读命令
    /// </summary>
    public async Task<ushort[]> SendReadCommandAsync(ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => SendReadCommand(startAddress, quantity), cancellationToken);
    }

    /// <summary>
    /// 异步发送原始数据
    /// </summary>
    public async Task<byte[]> SendRawAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => SendRaw(data), cancellationToken);
    }

    #endregion

    #region 事件触发

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
            Disconnect();
            _disposed = true;
        }
    }

    #endregion
}
