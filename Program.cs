using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;

namespace SystemInfo
{
    class Program
    {
        // 全局报告缓存，用于最后保存到桌面
        private static StringBuilder _reportCache = new StringBuilder();

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "系统信息检测工具（数智与信息化发展部V2026.0.1）";

            while (true)
            {
                Console.Clear();
                Console.WriteLine("========================================");
                Console.WriteLine("   系统信息检测工具（数智与信息化发展部V2026.0.1）");
                Console.WriteLine("========================================");
                Console.WriteLine("【核心亮点】");
                Console.WriteLine("  ✓ 支持基础/完整硬件配置检测");
                Console.WriteLine("  ✓ 实时输出检测结果（每完成一项立即显示）");
                Console.WriteLine("  ✓ 一键保存报告到桌面（文件名含计算机名和时间戳）");
                Console.WriteLine("  ✓ 涵盖主板、BIOS、网卡、声卡、电池、显示器等详细信息");
                Console.WriteLine("  ✓ 品牌中文识别 + 出厂日期本地检测 + 信创产品判断");
                Console.WriteLine("========================================");
                Console.WriteLine("请选择操作：");
                Console.WriteLine("  1 - 检测基础配置信息");
                Console.WriteLine("  2 - 检测完整配置信息");
                Console.WriteLine("  0 - 退出");
                Console.Write("请输入选项 (0-2): ");

                string choice = Console.ReadLine();

                if (choice == "0")
                {
                    Console.WriteLine("程序退出。");
                    break;
                }

                // 清空报告缓存
                _reportCache.Clear();

                // 根据选项执行检测（实时输出）
                if (choice == "1")
                {
                    CollectBasicInfoRealtime();
                }
                else if (choice == "2")
                {
                    CollectFullInfoRealtime();
                }
                else
                {
                    Console.WriteLine("无效选项，按任意键重新选择...");
                    Console.ReadKey();
                    continue;
                }

                // 所有模块检测完成后，提示保存或返回
                Console.WriteLine("\n信息显示完毕。");
                Console.WriteLine("按 Enter 键保存结果到桌面，按 Esc 键返回主菜单...");

                while (true)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter)
                    {
                        SaveReportToDesktop();
                        Console.WriteLine("\n按任意键返回主菜单...");
                        Console.ReadKey();
                        break;
                    }
                    else if (key.Key == ConsoleKey.Escape)
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine("无效按键，请按 Enter 保存，或按 Esc 返回。");
                    }
                }
            }
        }

        #region 实时检测模块（每完成一项立即输出）
        static void CollectBasicInfoRealtime()
        {
            AppendSection("操作系统信息", AppendOperatingSystemInfo);
            AppendSection("计算机品牌与型号", AppendComputerSystemInfo);
            AppendSection("处理器信息", AppendProcessorInfo);
            AppendSection("内存信息", AppendMemoryInfo);
            AppendSection("磁盘信息", AppendDiskInfo);
            AppendSection("显卡信息", AppendVideoControllerInfo);
        }

        static void CollectFullInfoRealtime()
        {
            AppendSection("操作系统信息", AppendOperatingSystemInfo);
            AppendSection("计算机品牌与型号", AppendComputerSystemInfo);
            AppendSection("处理器信息", AppendProcessorInfo);
            AppendSection("内存信息", AppendMemoryInfo);
            AppendSection("磁盘信息", AppendDiskInfo);
            AppendSection("显卡信息", AppendVideoControllerInfo);
            AppendSection("主板信息", AppendMotherboardInfo);
            AppendSection("BIOS信息", AppendBiosInfo);
            AppendSection("网卡信息", AppendNetworkInfo);
            AppendSection("声卡信息", AppendSoundDeviceInfo);
            AppendSection("电池信息", AppendBatteryInfo);
            AppendSection("显示器信息", AppendMonitorInfo);
        }

        /// <summary>
        /// 执行一个检测模块，实时输出并同时缓存内容
        /// </summary>
        static void AppendSection(string title, Action<StringBuilder> action)
        {
            // 显示当前正在检测的模块
            Console.WriteLine($"\n>>> 正在检测：{title} <<<\n");

            // 调用具体检测方法，将输出追加到 StringBuilder（用于缓存）
            var sb = new StringBuilder();
            action(sb);
            string content = sb.ToString();

            // 实时输出到控制台
            Console.Write(content);

            // 同时缓存到全局报告（加上标题和换行）
            _reportCache.AppendLine($"========== {title} ==========");
            _reportCache.Append(content);
            _reportCache.AppendLine();
        }
        #endregion

        #region 具体硬件信息采集方法（输出到 StringBuilder）
        static void AppendOperatingSystemInfo(StringBuilder sb)
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject os in searcher.Get())
                {
                    sb.AppendLine($"操作系统名称: {os["Caption"]}");
                    sb.AppendLine($"版本: {os["Version"]}");
                    if (os["InstallDate"] != null)
                        sb.AppendLine($"安装日期: {ManagementDateTimeConverter.ToDateTime(os["InstallDate"].ToString())}");
                    sb.AppendLine($"最后启动时间: {ManagementDateTimeConverter.ToDateTime(os["LastBootUpTime"].ToString())}");
                }
            }
        }

        static void AppendComputerSystemInfo(StringBuilder sb)
        {
            string manufacturer = "";
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
            {
                foreach (ManagementObject cs in searcher.Get())
                {
                    manufacturer = cs["Manufacturer"]?.ToString().Trim() ?? "未知";
                    string model = cs["Model"]?.ToString().Trim() ?? "未知";
                    string manufacturerCn = GetChineseBrandName(manufacturer);
                    sb.AppendLine($"制造商: {manufacturer}（{manufacturerCn}）");
                    sb.AppendLine($"型号: {model}");
                }
            }
            string serialNumber = "";
            string uuid = "";
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystemProduct"))
            {
                foreach (ManagementObject prod in searcher.Get())
                {
                    serialNumber = prod["IdentifyingNumber"]?.ToString().Trim() ?? "未知";
                    uuid = prod["UUID"]?.ToString().Trim() ?? "未知";
                    sb.AppendLine($"序列号: {serialNumber}");
                    sb.AppendLine($"UUID: {uuid}");
                }
            }

            // 出厂日期参考
            AppendManufactureDateInfo(sb, manufacturer, serialNumber);
            // 信创产品判断
            AppendXinChuangInfo(sb, manufacturer);
        }

        static string GetChineseBrandName(string manufacturer)
        {
            if (manufacturer.Contains("LENOVO") || manufacturer.Contains("联想")) return "联想";
            if (manufacturer.Contains("Dell") || manufacturer.Contains("戴尔")) return "戴尔";
            if (manufacturer.Contains("HP") || manufacturer.Contains("Hewlett-Packard") || manufacturer.Contains("惠普")) return "惠普";
            if (manufacturer.Contains("ASUS") || manufacturer.Contains("华硕")) return "华硕";
            if (manufacturer.Contains("Xiaomi") || manufacturer.Contains("小米")) return "小米";
            if (manufacturer.Contains("Huawei") || manufacturer.Contains("华为")) return "华为";
            if (manufacturer.Contains("Acer") || manufacturer.Contains("宏碁")) return "宏碁";
            if (manufacturer.Contains("Hasee") || manufacturer.Contains("神州")) return "神州";
            if (manufacturer.Contains("Microsoft") || manufacturer.Contains("微软")) return "微软";
            if (manufacturer.Contains("Apple") || manufacturer.Contains("苹果")) return "苹果";
            return "其他";
        }

        static void AppendManufactureDateInfo(StringBuilder sb, string manufacturer, string serialNumber)
        {
            sb.AppendLine("\n========== 出厂日期参考（本地检测） ==========");
            DateTime? manufactureDate = null;
            string detectionMethod = "";

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT ReleaseDate FROM Win32_BIOS"))
                {
                    foreach (ManagementObject bios in searcher.Get())
                    {
                        if (bios["ReleaseDate"] != null)
                        {
                            string dateStr = bios["ReleaseDate"].ToString();
                            if (dateStr.Length >= 14)
                            {
                                int year = int.Parse(dateStr.Substring(0, 4));
                                int month = int.Parse(dateStr.Substring(4, 2));
                                int day = int.Parse(dateStr.Substring(6, 2));
                                manufactureDate = new DateTime(year, month, day);
                                detectionMethod = "BIOS 发布日期";
                                break;
                            }
                        }
                    }
                }
            }
            catch { }

            if (manufactureDate.HasValue)
            {
                sb.AppendLine($"检测方法: {detectionMethod}");
                sb.AppendLine($"参考出厂日期: {manufactureDate.Value:yyyy-MM-dd}");
                int yearsDiff = DateTime.Now.Year - manufactureDate.Value.Year;
                if (yearsDiff > 10 || yearsDiff < 0)
                {
                    sb.AppendLine($"⚠ 警告: 检测到的日期与当前日期相差 {Math.Abs(yearsDiff)} 年，可能不准确！");
                    sb.AppendLine("建议手动到品牌官网使用序列号核实出厂日期：");
                    string officialUrl = GetOfficialWarrantyUrl(manufacturer);
                    if (!string.IsNullOrEmpty(officialUrl))
                        sb.AppendLine($"   官方售后查询链接: {officialUrl}");
                }
                else
                {
                    sb.AppendLine($"状态: 日期合理（与当前年份相差 {yearsDiff} 年）");
                }
            }
            else
            {
                sb.AppendLine("无法自动检测出厂日期。");
                sb.AppendLine("建议手动到品牌官网使用序列号核实：");
                string officialUrl = GetOfficialWarrantyUrl(manufacturer);
                if (!string.IsNullOrEmpty(officialUrl))
                    sb.AppendLine($"   官方售后查询链接: {officialUrl}");
            }
        }

        static string GetOfficialWarrantyUrl(string manufacturer)
        {
            if (manufacturer.Contains("LENOVO")) return "https://pcsupport.lenovo.com/";
            if (manufacturer.Contains("Dell")) return "https://www.dell.com/support/home/zh-cn";
            if (manufacturer.Contains("HP")) return "https://support.hp.com/cn-zh/check-warranty";
            if (manufacturer.Contains("ASUS")) return "https://www.asus.com.cn/support/warranty-status/";
            if (manufacturer.Contains("Xiaomi")) return "https://www.mi.com/service/imei";
            if (manufacturer.Contains("Huawei")) return "https://consumer.huawei.com/cn/support/warranty-query/";
            if (manufacturer.Contains("Acer")) return "https://www.acer.com.cn/support/warranty";
            return "";
        }

        static void AppendXinChuangInfo(StringBuilder sb, string manufacturer)
        {
            sb.AppendLine("\n========== 信创产品判断 ==========");
            bool isXinChuang = false;
            string reason = "";
            string[] domesticBrands = { "联想", "LENOVO", "华为", "Huawei", "小米", "Xiaomi", "神州", "Hasee", "浪潮", "Inspur", "中科曙光", "Sugon" };
            bool isDomesticBrand = false;
            foreach (var brand in domesticBrands)
            {
                if (manufacturer.Contains(brand))
                {
                    isDomesticBrand = true;
                    break;
                }
            }

            string cpuName = "";
            bool isDomesticCpu = false;
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                {
                    foreach (ManagementObject cpu in searcher.Get())
                    {
                        cpuName = cpu["Name"]?.ToString() ?? "";
                        break;
                    }
                }
                string[] domesticCpuKeywords = { "兆芯", "海光", "飞腾", "龙芯", "申威", "Hygon", "Phytium", "Loongson", "SW" };
                foreach (var kw in domesticCpuKeywords)
                {
                    if (cpuName.Contains(kw))
                    {
                        isDomesticCpu = true;
                        break;
                    }
                }
            }
            catch { }

            if (isDomesticBrand && isDomesticCpu)
            {
                isXinChuang = true;
                reason = "品牌为国产且 CPU 为国产";
            }
            else if (isDomesticBrand && !isDomesticCpu)
            {
                isXinChuang = false;
                reason = "品牌为国产但 CPU 非国产（可能是组装机或非信创配置）";
            }
            else if (!isDomesticBrand && isDomesticCpu)
            {
                isXinChuang = false;
                reason = "CPU 为国产但品牌非国产（少见）";
            }
            else
            {
                isXinChuang = false;
                reason = "品牌非国产且 CPU 非国产";
            }

            sb.AppendLine($"品牌: {manufacturer}");
            sb.AppendLine($"CPU: {cpuName}");
            sb.AppendLine($"判断结果: {(isXinChuang ? "是信创产品" : "非信创产品")}");
            sb.AppendLine($"判断依据: {reason}");
        }

        static void AppendProcessorInfo(StringBuilder sb)
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
            {
                foreach (ManagementObject cpu in searcher.Get())
                {
                    sb.AppendLine($"名称: {cpu["Name"]}");
                    sb.AppendLine($"核心数: {cpu["NumberOfCores"]}");
                    sb.AppendLine($"逻辑处理器: {cpu["NumberOfLogicalProcessors"]}");
                    sb.AppendLine($"最大时钟频率: {cpu["MaxClockSpeed"]} MHz");
                }
            }
        }

        static void AppendMemoryInfo(StringBuilder sb)
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory"))
            {
                double totalCapacity = 0;
                int slotIndex = 0;
                foreach (ManagementObject mem in searcher.Get())
                {
                    double capacity = Convert.ToDouble(mem["Capacity"]) / (1024 * 1024 * 1024);
                    totalCapacity += capacity;
                    slotIndex++;
                    sb.AppendLine($"插槽{slotIndex}: {capacity:F2} GB");
                }
                sb.AppendLine($"总内存: {totalCapacity:F2} GB");
                sb.AppendLine($"插槽数量: {slotIndex}");
            }
        }

        static void AppendDiskInfo(StringBuilder sb)
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive"))
            {
                int diskIndex = 0;
                foreach (ManagementObject disk in searcher.Get())
                {
                    diskIndex++;
                    double size = Convert.ToDouble(disk["Size"]) / (1024 * 1024 * 1024);
                    sb.AppendLine($"物理磁盘{diskIndex}: {disk["Model"]} ({size:F2} GB)");
                }
            }
            sb.AppendLine();

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk"))
            {
                foreach (ManagementObject ld in searcher.Get())
                {
                    string deviceId = ld["DeviceID"].ToString();
                    string fileSystem = ld["FileSystem"]?.ToString() ?? "未知";
                    double total = Convert.ToDouble(ld["Size"]) / (1024 * 1024 * 1024);
                    double free = Convert.ToDouble(ld["FreeSpace"]) / (1024 * 1024 * 1024);
                    double used = total - free;
                    double percent = total > 0 ? (used / total) * 100 : 0;
                    sb.AppendLine($"逻辑磁盘 {deviceId}: {fileSystem}  容量: {total:F2} GB  已用: {used:F2} GB ({percent:F1}% 已用) 可用: {free:F2} GB");
                }
            }
        }

        static void AppendVideoControllerInfo(StringBuilder sb)
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
            {
                int index = 0;
                foreach (ManagementObject vc in searcher.Get())
                {
                    index++;
                    string name = vc["Name"]?.ToString() ?? "未知";
                    string ram = "未知";
                    if (vc["AdapterRAM"] != null)
                    {
                        double ramMB = Convert.ToDouble(vc["AdapterRAM"]) / (1024 * 1024);
                        ram = $"{ramMB:F0} MB";
                    }
                    sb.AppendLine($"显卡{index}: {name} (专用内存: {ram})");
                }
            }
        }

        static void AppendMotherboardInfo(StringBuilder sb)
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard"))
            {
                foreach (ManagementObject mb in searcher.Get())
                {
                    sb.AppendLine($"制造商: {mb["Manufacturer"]}");
                    sb.AppendLine($"产品: {mb["Product"]}");
                    sb.AppendLine($"版本: {mb["Version"]}");
                    sb.AppendLine($"序列号: {mb["SerialNumber"]}");
                }
            }
        }

        static void AppendBiosInfo(StringBuilder sb)
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS"))
            {
                foreach (ManagementObject bios in searcher.Get())
                {
                    sb.AppendLine($"名称: {bios["Name"]}");
                    sb.AppendLine($"版本: {bios["SMBIOSBIOSVersion"]}");
                    sb.AppendLine($"序列号: {bios["SerialNumber"]}");
                    if (bios["ReleaseDate"] != null)
                        sb.AppendLine($"发布日期: {ManagementDateTimeConverter.ToDateTime(bios["ReleaseDate"].ToString())}");
                }
            }
        }

        static void AppendNetworkInfo(StringBuilder sb)
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = True"))
            {
                int index = 0;
                foreach (ManagementObject nic in searcher.Get())
                {
                    index++;
                    string description = nic["Description"]?.ToString() ?? "未知";
                    string mac = nic["MACAddress"]?.ToString() ?? "未知";
                    sb.AppendLine($"网卡{index}: {description}");
                    sb.AppendLine($"   MAC地址: {mac}");
                    if (nic["IPAddress"] != null)
                    {
                        string[] ips = (string[])nic["IPAddress"];
                        if (ips.Length > 0)
                            sb.AppendLine($"   IP地址: {string.Join(", ", ips)}");
                    }
                    if (nic["IPSubnet"] != null)
                    {
                        string[] subnets = (string[])nic["IPSubnet"];
                        if (subnets.Length > 0)
                            sb.AppendLine($"   子网掩码: {string.Join(", ", subnets)}");
                    }
                    if (nic["DefaultIPGateway"] != null)
                    {
                        string[] gateways = (string[])nic["DefaultIPGateway"];
                        if (gateways.Length > 0)
                            sb.AppendLine($"   默认网关: {string.Join(", ", gateways)}");
                    }
                }
            }
        }

        static void AppendSoundDeviceInfo(StringBuilder sb)
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_SoundDevice"))
            {
                int index = 0;
                foreach (ManagementObject sound in searcher.Get())
                {
                    index++;
                    string name = sound["Name"]?.ToString() ?? "未知";
                    string manufacturer = sound["Manufacturer"]?.ToString() ?? "未知";
                    sb.AppendLine($"声卡{index}: {name} (制造商: {manufacturer})");
                }
            }
        }

        static void AppendBatteryInfo(StringBuilder sb)
        {
            bool hasBattery = false;
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery"))
            {
                foreach (ManagementObject battery in searcher.Get())
                {
                    hasBattery = true;
                    sb.AppendLine($"名称: {battery["Name"]}");
                    sb.AppendLine($"预计剩余时间(分钟): {battery["EstimatedRunTime"]}");
                    sb.AppendLine($"电量状态: {battery["BatteryStatus"]}");
                }
            }
            if (!hasBattery)
            {
                sb.AppendLine("未检测到电池（可能为台式机或无电池设备）。");
            }
        }

        static void AppendMonitorInfo(StringBuilder sb)
        {
            int index = 0;
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DesktopMonitor"))
            {
                foreach (ManagementObject monitor in searcher.Get())
                {
                    index++;
                    string name = monitor["Name"]?.ToString() ?? "未知";
                    string screenHeight = monitor["ScreenHeight"]?.ToString() ?? "未知";
                    string screenWidth = monitor["ScreenWidth"]?.ToString() ?? "未知";
                    sb.AppendLine($"显示器{index}: {name}");
                    sb.AppendLine($"   分辨率: {screenWidth} x {screenHeight}");
                }
            }
            if (index == 0)
            {
                sb.AppendLine("未检测到显示器信息（可能通过其他方式连接）。");
            }
        }
        #endregion

        #region 保存报告到桌面
        static void SaveReportToDesktop()
        {
            try
            {
                string computerName = Environment.MachineName;
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = $"系统检测报告_{computerName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = Path.Combine(desktop, fileName);
                File.WriteAllText(filePath, _reportCache.ToString(), Encoding.UTF8);
                Console.WriteLine($"\n报告已保存至：{filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n保存文件失败：{ex.Message}");
            }
        }
        #endregion
    }
}