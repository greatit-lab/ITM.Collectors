// Collertor.cs
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace Onto.LampLifeCollector
{
    public struct LampInfo
    {
        public string LampId { get; set; }
        public string Age { get; set; }
        public string LifeSpan { get; set; }
        public string LastChanged { get; set; }
    }

    public class Collector
    {
        private const string PROCESS_NAME = "Main64";
        private const string OUTPUT_FOLDER = @"D:\CMP_DX\Temp";

        public void Execute()
        {
            CleanupOldLogs();

            var collectedLamps = new List<LampInfo>();

            try
            {
                var app = Application.Attach(PROCESS_NAME);
                using (var automation = new UIA3Automation())
                {
                    var mainWindow = app.GetMainWindow(automation);

                    // 1. System 버튼 클릭
                    var systemButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("25004"))?.AsButton();
                    if (systemButton == null)
                        throw new Exception("System button (25004) not found.");
                    systemButton.Click();
                    Thread.Sleep(500);

                    // 2. Tab(1111) → TabItem("Lamps") 클릭
                    var tabControl = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("1111"))?.AsTab();
                    if (tabControl == null)
                        throw new Exception("Tab control (1111) not found.");

                    var lampsTab = tabControl.FindFirstDescendant(cf =>
                        cf.ByName("Lamps").And(cf.ByControlType(ControlType.TabItem)));
                    if (lampsTab == null)
                        throw new Exception("TabItem 'Lamps' not found.");

                    lampsTab.Click();
                    Thread.Sleep(500);

                    // 3. Lamps 탭 안쪽의 List(10819) 찾기
                    var lampList = mainWindow.FindFirstDescendant(cf =>
                        cf.ByAutomationId("10819").And(cf.ByControlType(ControlType.List)));
                    if (lampList == null)
                        throw new Exception("List 'Lamp Status' (10819) not found.");

                    // 4. List(10819) 안의 모든 ListItem만 가져오기
                    var lampItems = lampList.FindAllChildren(cf => cf.ByControlType(ControlType.ListItem));
                    if (lampItems.Length == 0)
                        throw new Exception("No ListItems found inside List(10819).");

                    foreach (var item in lampItems)
                    {
                        var cells = item.FindAllChildren();

                        var newLamp = new LampInfo
                        {
                            LampId = item.Name,
                            Age = cells.FirstOrDefault(c => c.AutomationId == "ListViewSubItem-1")?.Name,
                            LifeSpan = cells.FirstOrDefault(c => c.AutomationId == "ListViewSubItem-2")?.Name,
                            LastChanged = cells.FirstOrDefault(c => c.AutomationId == "ListViewSubItem-4")?.Name
                        };

                        if (!string.IsNullOrEmpty(newLamp.LampId))
                            collectedLamps.Add(newLamp);
                    }

                    // 5. 수집된 데이터 저장
                    if (collectedLamps.Count > 0)
                    {
                        SaveDataToFile(collectedLamps);
                        Console.WriteLine($"Collected and saved {collectedLamps.Count} lamps.");
                    }
                    else
                    {
                        Console.WriteLine("Lamp items not found inside List(10819).");
                    }

                    // 6. 🔽 수집 후 Processing 버튼 클릭
                    var processingButton = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("25003"))?.AsButton();
                    if (processingButton != null)
                    {
                        processingButton.Click();
                        Console.WriteLine("Switched to 'Processing' screen (Button 25003 clicked).");
                    }
                    else
                    {
                        Console.WriteLine("Warning: 'Processing' button (25003) not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                string errorLogPath = Path.Combine(OUTPUT_FOLDER, "error.log");
                Directory.CreateDirectory(OUTPUT_FOLDER);
                File.AppendAllText(errorLogPath, $"{DateTime.Now}: {ex.Message}\n{ex.StackTrace}\n");
                Console.WriteLine($"Error occurred. Check {errorLogPath}.");
                throw;
            }
        }

        private void SaveDataToFile(List<LampInfo> lamps)
        {
            Directory.CreateDirectory(OUTPUT_FOLDER);
            string eqpid = Environment.MachineName;
            string fileName = $"{DateTime.Now:yyyyMMdd}_itmlt.log";
            string filePath = Path.Combine(OUTPUT_FOLDER, fileName);

            using (var writer = new StringWriter())
            {
                writer.WriteLine($"DateTime:{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                //writer.WriteLine($"EQPID:{eqpid}");
                writer.WriteLine();

                foreach (var lamp in lamps)
                {
                    writer.WriteLine($"[Lamp]");
                    writer.WriteLine($"Lamp_ID:{lamp.LampId}");
                    writer.WriteLine($"Age_Hour:{lamp.Age}");
                    writer.WriteLine($"LifeSpan_Hour:{lamp.LifeSpan}");
                    writer.WriteLine($"Last_Changed:{lamp.LastChanged}");
                    writer.WriteLine();
                }
                writer.WriteLine("--------------------------------------------------");
                writer.WriteLine();

                File.AppendAllText(filePath, writer.ToString());
            }
        }

        private void CleanupOldLogs()
        {
            try
            {
                Directory.CreateDirectory(OUTPUT_FOLDER);
                var today = DateTime.Today;

                var logFiles = Directory.GetFiles(OUTPUT_FOLDER, "*_itmlt.log");
                foreach (var file in logFiles)
                {
                    string fileName = Path.GetFileName(file);
                    if (fileName.Length >= 8 &&
                        DateTime.TryParseExact(fileName.Substring(0, 8), "yyyyMMdd",
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out var fileDate))
                    {
                        if (fileDate.Date < today)
                        {
                            File.Delete(file);
                            Console.WriteLine($"Deleted old log: {fileName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorLogPath = Path.Combine(OUTPUT_FOLDER, "error.log");
                File.AppendAllText(errorLogPath, $"{DateTime.Now}: Log cleanup error: {ex.Message}\n");
                Console.WriteLine("Error during log cleanup. See error.log.");
            }
        }
    }
}
