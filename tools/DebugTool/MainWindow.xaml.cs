using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using RGASDK;

namespace RGASDK.DebugTool;

public partial class MainWindow : Window
{
    private RGACLient? _client;
    private bool _isConnected;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshPorts();
        AddLog("调试工具已启动，请先连接设备");
    }

    private void RefreshPorts()
    {
        var ports = RGACommunicator.GetAvailablePorts();
        cmbPorts.Items.Clear();
        foreach (var port in ports)
        {
            cmbPorts.Items.Add(port);
        }
        if (cmbPorts.Items.Count > 0)
        {
            cmbPorts.SelectedIndex = 0;
        }
    }

    private void AddLog(string message)
    {
        Dispatcher.Invoke(() =>
        {
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            lstLog.Items.Insert(0, logEntry);

            if (chkAutoScroll.IsChecked == true && lstLog.Items.Count > 100)
            {
                lstLog.Items.RemoveAt(lstLog.Items.Count - 1);
            }
        });
    }

    private void UpdateStatus(string status, bool isError = false)
    {
        Dispatcher.Invoke(() =>
        {
            txtStatus.Text = status;
            txtStatus.Foreground = isError ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Green;
        });
    }

    private ushort GetSelectedTag(ComboBox comboBox)
    {
        if (comboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            return ushort.Parse(item.Tag.ToString()!);
        }
        return 1;
    }

    private string GetText(TextBox textBox) => textBox.Text;
    private int GetSelectedIndex(ComboBox comboBox) => comboBox.SelectedIndex;
    private T GetNumericValue<T>(TextBox textBox) where T : struct
    {
        if (typeof(T) == typeof(uint))
        {
            if (uint.TryParse(textBox.Text, out uint val)) return (T)(object)val;
        }
        else if (typeof(T) == typeof(double))
        {
            if (double.TryParse(textBox.Text, out double val)) return (T)(object)val;
        }
        else if (typeof(T) == typeof(ushort))
        {
            if (ushort.TryParse(textBox.Text, out ushort val)) return (T)(object)val;
        }
        return default;
    }

    #region 连接管理

    private void BtnConnect_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var portName = cmbPorts.Text;
            if (string.IsNullOrWhiteSpace(portName))
            {
                AddLog("错误: 请选择或输入串口名称");
                return;
            }

            int baudRate = 921600;
            if (cmbBaudRate.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                baudRate = int.Parse(item.Tag.ToString()!);
            }

            ushort slaveAddress = 1;
            if (!string.IsNullOrWhiteSpace(txtSlaveAddress.Text))
            {
                ushort.TryParse(txtSlaveAddress.Text, out slaveAddress);
            }

            _client = new RGACLient(portName, baudRate);
            _client.SlaveAddress = (byte)slaveAddress;
            _client.LogReceived += (s, msg) => AddLog(msg);
            _client.ErrorOccurred += (s, ex) => AddLog($"错误: {ex.Message}");

            _isConnected = true;
            btnConnect.IsEnabled = false;
            btnDisconnect.IsEnabled = true;

            AddLog($"已连接到 {portName} @ {baudRate} bps, 从机地址: {slaveAddress}");
            UpdateStatus("已连接");
        }
        catch (Exception ex)
        {
            AddLog($"连接失败: {ex.Message}");
            UpdateStatus("连接失败", true);
        }
    }

    private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _client?.Dispose();
            _client = null;
            _isConnected = false;
            btnConnect.IsEnabled = true;
            btnDisconnect.IsEnabled = false;
            AddLog("已断开连接");
            UpdateStatus("未连接");
        }
        catch (Exception ex)
        {
            AddLog($"断开连接失败: {ex.Message}");
        }
    }

    #endregion

    #region RF控制

    private void BtnSetRFFrequency_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        uint frequency = GetNumericValue<uint>(txtRFFrequency);
        if (frequency == 0)
        {
            AddLog("错误: 无效的频率值");
            return;
        }

        Task.Run(() =>
        {
            try
            {
                var result = _client!.SetRFFrequency(frequency);
                Dispatcher.Invoke(() => AddLog(result.Success ? $"设置RF频率成功: {frequency} Hz" : $"设置失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"设置RF频率失败: {ex.Message}"));
            }
        });
    }

    private void BtnSetRFCenterVoltage_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        double voltage = GetNumericValue<double>(txtRFCenterVoltage);
        if (voltage == 0)
        {
            AddLog("错误: 无效的电压值");
            return;
        }

        Task.Run(() =>
        {
            try
            {
                var result = _client!.SetRFCenterVoltage(voltage);
                Dispatcher.Invoke(() => AddLog(result.Success ? $"设置RF中心电压成功: {voltage} V" : $"设置失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"设置RF中心电压失败: {ex.Message}"));
            }
        });
    }

    private void BtnDisableRF_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        Task.Run(() =>
        {
            try
            {
                var result = _client!.DisableRFVoltage();
                Dispatcher.Invoke(() => AddLog(result.Success ? "禁用RF电压成功" : $"禁用失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"禁用RF电压失败: {ex.Message}"));
            }
        });
    }

    private void BtnEnableRF_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        Task.Run(() =>
        {
            try
            {
                var result = _client!.EnableRFVoltage();
                Dispatcher.Invoke(() => AddLog(result.Success ? "使能RF电压成功" : $"使能失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"使能RF电压失败: {ex.Message}"));
            }
        });
    }

    private void BtnSetRFPhase_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        Task.Run(() =>
        {
            try
            {
                var phase = GetSelectedTag(cmbRFPhase) == 1 ? RFPhaseMode.Positive : RFPhaseMode.Negative;
                var result = _client!.SetRFPhase(phase);
                Dispatcher.Invoke(() => AddLog(result.Success ? $"设置RF相位成功: {(phase == RFPhaseMode.Positive ? "正极性" : "负极性")}" : $"设置失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"设置RF相位失败: {ex.Message}"));
            }
        });
    }

    private void BtnSetRFCurrentRange_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        Task.Run(() =>
        {
            try
            {
                var range = GetSelectedTag(cmbRFCurrentRange) == 1 ? RFCurrentRange.mA2 : RFCurrentRange.mA5;
                var result = _client!.SetRFCurrentRange(range);
                Dispatcher.Invoke(() => AddLog(result.Success ? $"设置RF电流档位成功: {(range == RFCurrentRange.mA2 ? "2mA" : "5mA")}" : $"设置失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"设置RF电流档位失败: {ex.Message}"));
            }
        });
    }

    private void BtnReadRFStatus_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        Task.Run(() =>
        {
            try
            {
                var result = _client!.ReadRFFrequency();
                Dispatcher.Invoke(() =>
                {
                    if (result.Success)
                    {
                        txtRFFreqFeedback.Text = result.Value.ToString();
                        AddLog($"读取RF频率成功");
                    }
                    else
                    {
                        AddLog($"读取RF频率失败: {result.ErrorMessage}");
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"读取RF状态失败: {ex.Message}"));
            }
        });
    }

    #endregion

    #region 扫描控制

    private void BtnSetScanParams_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        try
        {
            var scanParams = new ScanParameters
            {
                SegmentCount = GetNumericValue<ushort>(txtScanSegmentCount),
                StartMassNumber = GetNumericValue<ushort>(txtScanStartMass),
                StepCount = GetNumericValue<ushort>(txtScanStepCount),
                DwellTime = GetNumericValue<ushort>(txtScanDwellTime)
            };

            Task.Run(() =>
            {
                try
                {
                    var result = _client!.SetScanParameters(scanParams);
                    Dispatcher.Invoke(() => AddLog(result.Success ? "设置扫描参数成功" : $"设置失败: {result.ErrorMessage}"));
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => AddLog($"设置扫描参数失败: {ex.Message}"));
                }
            });
        }
        catch
        {
            AddLog("错误: 无效的扫描参数");
        }
    }

    private void BtnStartScan_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        try
        {
            var scanConfig = new ScanConfig
            {
                ScanSegment = GetNumericValue<ushort>(txtScanSegment),
                LoopCount = GetNumericValue<ushort>(txtScanLoopCount),
                SettlingTime = GetNumericValue<ushort>(txtScanSettlingTime),
                SegmentInterval = GetNumericValue<ushort>(txtScanInterval)
            };

            Task.Run(() =>
            {
                try
                {
                    var result = _client!.StartScan(scanConfig);
                    Dispatcher.Invoke(() => AddLog(result.Success ? "开始扫描成功" : $"开始扫描失败: {result.ErrorMessage}"));
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => AddLog($"开始扫描失败: {ex.Message}"));
                }
            });
        }
        catch
        {
            AddLog("错误: 无效的扫描配置");
        }
    }

    private void BtnStopScan_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        Task.Run(() =>
        {
            try
            {
                var result = _client!.StopScan();
                Dispatcher.Invoke(() => AddLog(result.Success ? "停止扫描成功" : $"停止扫描失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"停止扫描失败: {ex.Message}"));
            }
        });
    }

    private void BtnReadScanData_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        try
        {
            double startMass = GetNumericValue<double>(txtReadStartMass);
            double endMass = GetNumericValue<double>(txtReadEndMass);
            double stepSize = GetNumericValue<double>(txtReadStepSize);

            Task.Run(() =>
            {
                try
                {
                    var result = _client!.ReadMassScanData(startMass, endMass, stepSize);
                    Dispatcher.Invoke(() =>
                    {
                        if (result.Success)
                        {
                            dgScanData.ItemsSource = result.Value;
                            AddLog($"读取扫描数据成功，共 {result.Value.Count} 个数据点");
                        }
                        else
                        {
                            AddLog($"读取扫描数据失败: {result.ErrorMessage}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => AddLog($"读取扫描数据失败: {ex.Message}"));
                }
            });
        }
        catch
        {
            AddLog("错误: 无效的扫描数据参数");
        }
    }

    #endregion

    #region 灯丝控制

    private void BtnSelectFilament_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        var filamentTag = GetSelectedTag(cmbFilamentSelect);

        Task.Run(() =>
        {
            try
            {
                var filament = filamentTag == 1 ? FilamentSelect.FilamentA : FilamentSelect.FilamentB;
                var result = _client!.SelectFilament(filament);
                Dispatcher.Invoke(() => AddLog(result.Success ? $"选择灯丝成功: {(filament == FilamentSelect.FilamentA ? "灯丝A" : "灯丝B")}" : $"选择失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"选择灯丝失败: {ex.Message}"));
            }
        });
    }

    private void BtnSetFilamentEmission_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        ushort current = GetNumericValue<ushort>(txtFilamentEmission);
        if (current == 0)
        {
            AddLog("错误: 无效的电流值");
            return;
        }

        Task.Run(() =>
        {
            try
            {
                var result = _client!.SetFilamentEmission(current);
                Dispatcher.Invoke(() => AddLog(result.Success ? $"设置发射电流成功: {current} uA" : $"设置失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"设置发射电流失败: {ex.Message}"));
            }
        });
    }

    private void BtnSetDischargeTime_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        ushort time = GetNumericValue<ushort>(txtDischargeTime);
        if (time == 0)
        {
            AddLog("错误: 无效的时间值");
            return;
        }

        Task.Run(() =>
        {
            try
            {
                var result = _client!.SetDischargeTime(time);
                Dispatcher.Invoke(() => AddLog(result.Success ? $"设置放电时间成功: {time} us" : $"设置失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"设置放电时间失败: {ex.Message}"));
            }
        });
    }

    private void BtnSetFilamentSafety_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        double current = GetNumericValue<double>(txtFilamentSafety);
        if (current == 0)
        {
            AddLog("错误: 无效的电流值");
            return;
        }

        Task.Run(() =>
        {
            try
            {
                var result = _client!.SetFilamentCurrentSafety(current);
                Dispatcher.Invoke(() => AddLog(result.Success ? $"设置电流安全值成功: {current} A" : $"设置失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"设置电流安全值失败: {ex.Message}"));
            }
        });
    }

    #endregion

    #region EM板控制

    private void BtnSetDETHV_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        ushort voltage = GetNumericValue<ushort>(txtDETHV);
        if (voltage == 0)
        {
            AddLog("错误: 无效的电压值");
            return;
        }

        Task.Run(() =>
        {
            try
            {
                var result = _client!.SetDET_HVVoltage(voltage);
                Dispatcher.Invoke(() => AddLog(result.Success ? $"设置DET_HV电压成功: {voltage} V" : $"设置失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"设置DET_HV电压失败: {ex.Message}"));
            }
        });
    }

    private void BtnSetAnodeVoltage_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        ushort voltage = GetNumericValue<ushort>(txtAnodeVoltage);
        if (voltage == 0)
        {
            AddLog("错误: 无效的电压值");
            return;
        }

        Task.Run(() =>
        {
            try
            {
                var result = _client!.SetAnodeVoltage(voltage);
                Dispatcher.Invoke(() => AddLog(result.Success ? $"设置Anode电压成功: {voltage} V" : $"设置失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"设置Anode电压失败: {ex.Message}"));
            }
        });
    }

    private void BtnSetFocusVoltage_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        ushort voltage = GetNumericValue<ushort>(txtFocusVoltage);
        if (voltage == 0)
        {
            AddLog("错误: 无效的电压值");
            return;
        }

        Task.Run(() =>
        {
            try
            {
                var result = _client!.SetFocusVoltage(voltage);
                Dispatcher.Invoke(() => AddLog(result.Success ? $"设置Focus电压成功: {voltage} V" : $"设置失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"设置Focus电压失败: {ex.Message}"));
            }
        });
    }

    private void BtnSetElectronEnergy_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        ushort energy = GetNumericValue<ushort>(txtElectronEnergy);
        if (energy == 0)
        {
            AddLog("错误: 无效的能量值");
            return;
        }

        Task.Run(() =>
        {
            try
            {
                var result = _client!.SetElectronEnergy(energy);
                Dispatcher.Invoke(() => AddLog(result.Success ? $"设置ElectronEnergy成功: {energy} eV" : $"设置失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"设置ElectronEnergy失败: {ex.Message}"));
            }
        });
    }

    #endregion

    #region FPGA控制

    private void BtnSetOutB_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        ushort value = GetNumericValue<ushort>(txtOutB);
        if (value == 0)
        {
            AddLog("错误: 无效的值");
            return;
        }

        Task.Run(() =>
        {
            try
            {
                var result = _client!.SetDetectorOutB(value);
                Dispatcher.Invoke(() => AddLog(result.Success ? $"设置OutB成功: {value}" : $"设置失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"设置OutB失败: {ex.Message}"));
            }
        });
    }

    private void BtnSetOutC_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        ushort value = GetNumericValue<ushort>(txtOutC);
        if (value == 0)
        {
            AddLog("错误: 无效的值");
            return;
        }

        Task.Run(() =>
        {
            try
            {
                var result = _client!.SetOutCLower(value);
                Dispatcher.Invoke(() => AddLog(result.Success ? $"设置OutC成功: {value}" : $"设置失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"设置OutC失败: {ex.Message}"));
            }
        });
    }

    private void BtnSetOutD_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        ushort value = GetNumericValue<ushort>(txtOutD);
        if (value == 0)
        {
            AddLog("错误: 无效的值");
            return;
        }

        Task.Run(() =>
        {
            try
            {
                var result = _client!.SetOutDUpper(value);
                Dispatcher.Invoke(() => AddLog(result.Success ? $"设置OutD成功: {value}" : $"设置失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"设置OutD失败: {ex.Message}"));
            }
        });
    }

    #endregion

    #region 系统控制

    private void BtnSetBaudRate_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        var baudRateTag = GetSelectedTag(cmbSetBaudRate);

        Task.Run(() =>
        {
            try
            {
                var baudRate = baudRateTag switch
                {
                    1 => BaudRateSetting.Baud9600,
                    3 => BaudRateSetting.Baud921600,
                    _ => BaudRateSetting.Baud115200
                };

                var result = _client!.SetBaudRate(baudRate);
                Dispatcher.Invoke(() => AddLog(result.Success ? $"设置波特率成功" : $"设置失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"设置波特率失败: {ex.Message}"));
            }
        });
    }

    private void BtnSetVacuumCoeff_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        double coeff = GetNumericValue<double>(txtVacuumCoeff);
        if (coeff == 0)
        {
            AddLog("错误: 无效的系数值");
            return;
        }

        Task.Run(() =>
        {
            try
            {
                var result = _client!.SetVacuumCoefficient(coeff);
                Dispatcher.Invoke(() => AddLog(result.Success ? $"设置真空系数成功: {coeff}" : $"设置失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"设置真空系数失败: {ex.Message}"));
            }
        });
    }

    private void BtnSaveParams_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        Task.Run(() =>
        {
            try
            {
                var result = _client!.SaveParameters();
                Dispatcher.Invoke(() => AddLog(result.Success ? "保存参数成功" : $"保存失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"保存参数失败: {ex.Message}"));
            }
        });
    }

    private void BtnClearError_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        Task.Run(() =>
        {
            try
            {
                var result = _client!.ClearErrorFlag();
                Dispatcher.Invoke(() => AddLog(result.Success ? "清除错误标记成功" : $"清除失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"清除错误标记失败: {ex.Message}"));
            }
        });
    }

    private void BtnStartCalibration_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        Task.Run(() =>
        {
            try
            {
                var result = _client!.StartCalibration();
                Dispatcher.Invoke(() => AddLog(result.Success ? "开始校验成功" : $"开始校验失败: {result.ErrorMessage}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"开始校验失败: {ex.Message}"));
            }
        });
    }

    #endregion

    #region 读取操作

    private void BtnReadStatus_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        Task.Run(() =>
        {
            try
            {
                var result = _client!.ReadStatus();
                Dispatcher.Invoke(() =>
                {
                    if (result.Success && result.Value != null)
                    {
                        var status = result.Value;
                        var info = $@"设备状态:
真空度: {status.VacuumDegree:E2} Torr
Anode电压: {status.AnodeVoltage:F2} V
EM电压: {status.EMVoltage:F2} V
灯丝电流: {status.FilamentCurrent:F3} A
RF频率反馈: {status.RF_Fb1} Hz
Focus电压: {status.FocusVoltage}
ElectronEnergy: {status.ElectronEnergy:F1} eV
Avcc(24V): {status.AvccVoltage:F2} V
Vcc1(5V): {status.Vcc1Voltage:F2} V
Vcc2(3.3V): {status.Vcc2Voltage:F2} V";
                        AddLog(info);
                    }
                    else
                    {
                        AddLog($"读取状态失败: {result.ErrorMessage}");
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"读取状态失败: {ex.Message}"));
            }
        });
    }

    private void BtnReadPSHeartbeat_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        Task.Run(() =>
        {
            try
            {
                var result = _client!.ReadPSHeartbeat();
                Dispatcher.Invoke(() =>
                {
                    if (result.Success)
                    {
                        txtPSHeartbeat.Text = result.Value.ToString();
                        AddLog($"PS板心跳: {result.Value}");
                    }
                    else
                    {
                        AddLog($"读取失败: {result.ErrorMessage}");
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"读取PS心跳失败: {ex.Message}"));
            }
        });
    }

    private void BtnReadSYSHeartbeat_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        Task.Run(() =>
        {
            try
            {
                var result = _client!.ReadSYSHeartbeat();
                Dispatcher.Invoke(() =>
                {
                    if (result.Success)
                    {
                        txtSYSHeartbeat.Text = result.Value.ToString();
                        AddLog($"SYS板心跳: {result.Value}");
                    }
                    else
                    {
                        AddLog($"读取失败: {result.ErrorMessage}");
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"读取SYS心跳失败: {ex.Message}"));
            }
        });
    }

    private void BtnReadVacuum_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        Task.Run(() =>
        {
            try
            {
                var result = _client!.ReadVacuumDegree();
                Dispatcher.Invoke(() =>
                {
                    if (result.Success)
                    {
                        txtVacuum.Text = $"{result.Value:E2}";
                        AddLog($"真空度: {result.Value:E2} Torr");
                    }
                    else
                    {
                        AddLog($"读取失败: {result.ErrorMessage}");
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"读取真空度失败: {ex.Message}"));
            }
        });
    }

    private void BtnReadRFFreq_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        Task.Run(() =>
        {
            try
            {
                var result = _client!.ReadRFFrequency();
                Dispatcher.Invoke(() =>
                {
                    if (result.Success)
                    {
                        txtRFFreq.Text = result.Value.ToString();
                        AddLog($"RF频率: {result.Value} Hz");
                    }
                    else
                    {
                        AddLog($"读取失败: {result.ErrorMessage}");
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"读取RF频率失败: {ex.Message}"));
            }
        });
    }

    private void BtnReadAnodeVoltage_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        Task.Run(() =>
        {
            try
            {
                var result = _client!.ReadAnodeVoltage();
                Dispatcher.Invoke(() =>
                {
                    if (result.Success)
                    {
                        txtAnodeVoltRead.Text = $"{result.Value:F2}";
                        AddLog($"Anode电压: {result.Value:F2} V");
                    }
                    else
                    {
                        AddLog($"读取失败: {result.ErrorMessage}");
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"读取Anode电压失败: {ex.Message}"));
            }
        });
    }

    private void BtnReadEMVoltage_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        Task.Run(() =>
        {
            try
            {
                var result = _client!.ReadEMVoltage();
                Dispatcher.Invoke(() =>
                {
                    if (result.Success)
                    {
                        txtEMVoltRead.Text = $"{result.Value:F2}";
                        AddLog($"EM电压: {result.Value:F2} V");
                    }
                    else
                    {
                        AddLog($"读取失败: {result.ErrorMessage}");
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"读取EM电压失败: {ex.Message}"));
            }
        });
    }

    private void BtnReadFilamentCurrent_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        Task.Run(() =>
        {
            try
            {
                var result = _client!.ReadFilamentCurrent();
                Dispatcher.Invoke(() =>
                {
                    if (result.Success)
                    {
                        txtFilamentCurr.Text = $"{result.Value:F3}";
                        AddLog($"灯丝电流: {result.Value:F3} A");
                    }
                    else
                    {
                        AddLog($"读取失败: {result.ErrorMessage}");
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"读取灯丝电流失败: {ex.Message}"));
            }
        });
    }

    private void BtnReadElectronEnergy_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        Task.Run(() =>
        {
            try
            {
                var result = _client!.ReadElectronEnergy();
                Dispatcher.Invoke(() =>
                {
                    if (result.Success)
                    {
                        txtElectronE.Text = $"{result.Value:F1}";
                        AddLog($"ElectronEnergy: {result.Value:F1} eV");
                    }
                    else
                    {
                        AddLog($"读取失败: {result.ErrorMessage}");
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"读取ElectronEnergy失败: {ex.Message}"));
            }
        });
    }

    #endregion

    #region 设备初始化

    private void BtnInitializeDevice_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        try
        {
            var emConfig = new EMBoardConfig
            {
                DET_HVVoltage = GetNumericValue<ushort>(txtInitDETHV),
                AnodeVoltage = GetNumericValue<ushort>(txtInitAnode),
                FocusVoltage = GetNumericValue<ushort>(txtInitFocus),
                ElectronEnergy = GetNumericValue<ushort>(txtInitElectronE)
            };

            var rfConfig = new RFConfig
            {
                Frequency = GetNumericValue<uint>(txtInitFreq),
                CenterVoltage = GetNumericValue<double>(txtInitCenterV),
                PhaseMode = GetSelectedIndex(cmbInitPhase) == 0 ? RFPhaseMode.Positive : RFPhaseMode.Negative,
                CurrentRange = RFCurrentRange.mA2
            };

            var filamentConfig = new FilamentConfig
            {
                Filament = GetSelectedIndex(cmbInitFilament) == 0 ? FilamentSelect.FilamentA : FilamentSelect.FilamentB,
                EmissionCurrent = GetNumericValue<ushort>(txtInitEmission),
                DischargeTime = GetNumericValue<ushort>(txtInitDischarge)
            };

            Task.Run(() =>
            {
                try
                {
                    Dispatcher.Invoke(() => AddLog("开始设备初始化..."));

                    var result = _client!.InitializeDevice(emConfig, rfConfig, filamentConfig);

                    Dispatcher.Invoke(() =>
                    {
                        if (result.Success)
                        {
                            AddLog("设备初始化成功！");

                            var status = _client.ReadStatus();
                            if (status.Success && status.Value != null)
                            {
                                AddLog($"真空度: {status.Value.VacuumDegree:E2} Torr");
                            }
                        }
                        else
                        {
                            AddLog($"设备初始化失败: {result.ErrorMessage}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => AddLog($"设备初始化失败: {ex.Message}"));
                }
            });
        }
        catch
        {
            AddLog("错误: 无效的初始化参数");
        }
    }

    #endregion

    #region 原始数据发送

    private void BtnSendRaw_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckConnection()) return;

        string hexString;
        try
        {
            hexString = txtRawData.Text.Replace(" ", "").Replace("-", "");
        }
        catch
        {
            AddLog("错误: 无法读取输入数据");
            return;
        }

        Task.Run(() =>
        {
            try
            {
                if (hexString.Length % 2 != 0)
                {
                    Dispatcher.Invoke(() => AddLog("错误: 十六进制数据长度必须是偶数"));
                    return;
                }

                var data = new byte[hexString.Length / 2];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
                }

                var response = _client!.SendRaw(data);
                Dispatcher.Invoke(() =>
                {
                    if (response.Length > 0)
                    {
                        AddLog($"原始数据发送成功，收到 {response.Length} 字节: {RGAProtocolHandler.ToHexString(response)}");
                    }
                    else
                    {
                        AddLog("原始数据发送成功，但未收到响应");
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => AddLog($"发送原始数据失败: {ex.Message}"));
            }
        });
    }

    #endregion

    #region 日志

    private void BtnClearLog_Click(object sender, RoutedEventArgs e)
    {
        lstLog.Items.Clear();
        AddLog("日志已清空");
    }

    #endregion

    private bool CheckConnection()
    {
        if (_client == null || !_isConnected)
        {
            AddLog("错误: 请先连接设备");
            return false;
        }
        return true;
    }
}
