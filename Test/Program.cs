using System;
using RGASDK;

namespace RGASDK.Test
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("=== RGASDK 协议测试 ===\n");

            TestCRC();
            TestReadCommands();
            TestWriteCommands();

            Console.WriteLine("\n=== 测试完成 ===");
        }

        static void TestCRC()
        {
            Console.WriteLine("--- CRC校验测试 ---");

            // 根据文档: 读取PS心跳 01 03 00 0C 00 01 44 09
            // 验证: 对 01 03 00 0C 00 01 计算CRC
            byte[] readCmd = new byte[] { 0x01, 0x03, 0x00, 0x0C, 0x00, 0x01 };
            ushort crc = RGAProtocolHandler.CalculateCRC16(readCmd);
            Console.WriteLine($"读取PS心跳帧: 01 03 00 0C 00 01");
            Console.WriteLine($"计算CRC: {crc:X4}");
            Console.WriteLine($"期望CRC: 4409");
            Console.WriteLine($"验证结果: {(crc == 0x0944 ? "✓ 通过 (低字节在前)" : "✗ 失败")}\n");

            // 验证发送帧格式
            var protocol = new RGAProtocolHandler();
            var frame = protocol.BuildReadCommand(0x01, 0x000C, 1);
            Console.WriteLine($"构建读取帧: {RGAProtocolHandler.ToHexString(frame)}");
            Console.WriteLine($"文档帧:    01 03 00 0C 00 01 44 09");
            Console.WriteLine($"验证结果: {(RGAProtocolHandler.ToHexString(frame) == "01 03 00 0C 00 01 44 09" ? "✓ 通过" : "✗ 失败")}\n");
        }

        static void TestReadCommands()
        {
            Console.WriteLine("--- 读取命令帧验证 ---");
            var protocol = new RGAProtocolHandler();

            // 测试所有读取命令 - 只验证帧格式，不验证CRC
            var tests = new (string name, ushort address, ushort quantity)[]
            {
                ("PS心跳", 0x000C, 1),
                ("SYS心跳", 0x000B, 1),
                ("Anode电压", 0x0038, 2),
                ("EM电压", 0x002E, 2),
                ("灯丝激发电流", 0x003C, 2),
                ("Focus电压", 0x0041, 1),
                ("ElectronEnergy", 0x003E, 1),
                ("灯丝电压", 0x0034, 2),
                ("灯丝电流", 0x002C, 2),
                ("Anode电流", 0x002A, 2),
                ("真空度", 0x0014, 2),
            };

            foreach (var test in tests)
            {
                var frame = protocol.BuildReadCommand(0x01, test.address, test.quantity);
                Console.WriteLine($"{test.name} (地址:{test.address:X4}, 数量:{test.quantity}):");
                Console.WriteLine($"  {RGAProtocolHandler.ToHexString(frame)}");

                // 验证帧格式
                bool formatOk = frame[0] == 0x01 && frame[1] == 0x03;
                Console.WriteLine($"  格式: {(formatOk ? "✓" : "✗")}");
            }
            Console.WriteLine();
        }

        static void TestWriteCommands()
        {
            Console.WriteLine("--- 写命令测试 ---");

            // 根据文档: 设置RF频率 2000000Hz
            // 帧: 00 1E 84 80 表示频率 = (0x001E << 16) | 0x8480 = 2000000Hz
            var protocol = new RGAProtocolHandler();

            // 直接调试
            uint freq = 2000000;
            var data = RGACommands.SetRFFrequency(freq);
            Console.WriteLine($"频率: {freq} Hz");
            Console.WriteLine($"data[0] = 0x{data[0]:X4}");
            Console.WriteLine($"data[1] = 0x{data[1]:X4}");

            var command = protocol.BuildWriteCommand(
                0x01,                           // 从机地址
                0x0003,                         // 子系统地址: 控制板
                0x000A,                         // 子系统功能码: RF频率
                data
            );

            Console.WriteLine($"\n完整帧: {RGAProtocolHandler.ToHexString(command)}");
            Console.WriteLine($"帧长度: {command.Length} bytes");

            // 解析频率数据位置
            // 帧格式: 地址(1) + 功能码(1) + 起始地址(2) + 寄存器数(2) + 字节数(1) + 子系统地址(2) + 功能码(2) + 指令类型(2) = 13字节后是数据
            Console.WriteLine($"\n帧格式分析:");
            Console.WriteLine($"  从机地址: 0x{command[0]:X2}");
            Console.WriteLine($"  功能码: 0x{command[1]:X2}");
            Console.WriteLine($"  起始地址: 0x{command[2]:X2}{command[3]:X2}");
            Console.WriteLine($"  寄存器数: 0x{command[4]:X2}{command[5]:X2}");
            Console.WriteLine($"  字节数: 0x{command[6]:X2}");
            Console.WriteLine($"  子系统地址: 0x{command[7]:X2}{command[8]:X2}");
            Console.WriteLine($"  子系统功能码: 0x{command[9]:X2}{command[10]:X2}");
            Console.WriteLine($"  指令类型: 0x{command[11]:X2}{command[12]:X2}");

            // 数据从索引13开始 (12字节 = 6个寄存器)
            Console.WriteLine($"\n数据(13-24):");
            for (int i = 13; i < 25; i++) Console.Write($"{command[i]:X2} ");
            Console.WriteLine();

            // 频率数据: 索引13-16 (data[0]), 索引17-20 (data[1])
            Console.WriteLine($"\n频率数据:");
            Console.WriteLine($"  data[0] (索引13-14): {command[13]:X2} {command[14]:X2} = 0x{((command[13] << 8) | command[14]):X4}");
            Console.WriteLine($"  data[1] (索引15-16): {command[15]:X2} {command[16]:X2} = 0x{((command[15] << 8) | command[16]):X4}");
            Console.WriteLine($"  data[2] (索引17-18): {command[17]:X2} {command[18]:X2}");
            Console.WriteLine($"  data[3] (索引19-20): {command[19]:X2} {command[20]:X2}");

            // 验证帧格式
            bool frameMatch =
                command[13] == 0x00 && command[14] == 0x1E &&
                command[15] == 0x84 && command[16] == 0x80;
            Console.WriteLine($"\n帧格式: {(frameMatch ? "✓ 通过" : "✗ 失败")}");
            Console.WriteLine($"期望数据: 00 1E 84 80");
            Console.WriteLine($"实际数据: {command[13]:X2} {command[14]:X2} {command[15]:X2} {command[16]:X2}");
            Console.WriteLine();
        }

        static void TestWriteCommand(string name, RGAProtocolHandler protocol,
            ushort subsystem, ushort funcCode, ushort[] data)
        {
            var cmd = protocol.BuildWriteCommand(0x01, subsystem, funcCode, data);
            Console.WriteLine($"  {name}: {RGAProtocolHandler.ToHexString(cmd)}");
        }
    }
}
