using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using Console = Colorful.Console;

namespace HowestMarkRunner
{
    class Program
    {
        private static Color COLOR_BLUE = Color.FromArgb(68, 200, 244);
        private static Color COLOR_PINK = Color.FromArgb(236, 0, 140);
        private static Color COLOR_YELLOW = Color.FromArgb(255, 242, 0);

        static void Main(string[] args)
        {
            Config config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));


            Console.ForegroundColor = COLOR_BLUE;
            Console.Write("Howest");
            Console.ForegroundColor = COLOR_PINK;
            Console.Write("Mark");
            Console.ForegroundColor = Color.White;
            Console.WriteLine(" Runner");
            Console.WriteLine("-----------------");

            if (!IsAdministrator())
            {
                Console.ForegroundColor = COLOR_PINK;
                Console.WriteLine("Error: HowestMarkRunner must be run as Administrator for it to be able to assign realtime process priorities.");
                PressToExit();
            }

            Console.ForegroundColor = COLOR_YELLOW;
            Console.WriteLine("Executables:");
            Console.ForegroundColor = Color.White;

            bool erroredExecutables = false;
            foreach (ExecutableConfig executable in config.Executables)
            {
                if (!File.Exists(executable.Path))
                {
                    Console.WriteLine(executable.Path + " (not found!)");

                    erroredExecutables = true;
                }
                else
                {
                    Console.WriteLine(executable.Path);
                }
            }

            if (erroredExecutables)
            {
                Console.ForegroundColor = COLOR_PINK;
                Console.WriteLine("Error: Some executables weren't found, adjust the config and try again.");
                PressToExit();
            }

            Console.ForegroundColor = COLOR_YELLOW;
            Console.WriteLine("Tests:");
            Console.ForegroundColor = Color.White;
            foreach (string test in config.Tests)
            {
                Console.WriteLine(test);
            }

            Console.ForegroundColor = COLOR_YELLOW;
            Console.WriteLine("Executing automated tests:");
            var currentPos = Console.CursorTop;

            Console.ForegroundColor = Color.Gray;
            foreach (string test in config.Tests)
            {
                foreach (ExecutableConfig executable in config.Executables)
                {
                    Console.WriteLine(executable.Path + " - " + test);
                }
            }

            Console.CursorTop = currentPos;
            Console.ForegroundColor = Color.White;
            foreach (string test in config.Tests)
            {
                foreach (ExecutableConfig executable in config.Executables)
                {
                    var engineName = executable.EngineName;
                    Console.ForegroundColor = Color.White;

                    using (var runningTest = new Process())
                    {
                        var outputText = new StringBuilder();
                        var useStdout = executable.OutputMethod == "stdout";

                        runningTest.StartInfo.FileName = Path.Combine(Environment.CurrentDirectory, executable.Path);
                        runningTest.StartInfo.Arguments = executable.CommandlineArgs + (executable.CommandlineArgs == "" ? "" : " ") + "-test=" + test;
                        runningTest.StartInfo.RedirectStandardOutput = useStdout;
                        runningTest.StartInfo.UseShellExecute = !useStdout;

                        if (useStdout)
                            runningTest.OutputDataReceived += (_, dataEvent) =>
                            {
                                if (dataEvent.Data != null) {
                                    if (dataEvent.Data.StartsWith("suicide")) runningTest.Kill();
                                }

                                outputText.Append(dataEvent.Data + Environment.NewLine);
                            };

                        //var testInfo = executable.Path + " " + runningTest.StartInfo.Arguments + ", read data from: " + executable.OutputMethod;
                        Console.WriteLine(executable.Path + " - " + test);
                        
                        runningTest.Start();
                        runningTest.PriorityClass = ProcessPriorityClass.RealTime;
                        if (useStdout) runningTest.BeginOutputReadLine();
                        runningTest.WaitForExit();

                        if (!useStdout)
                        {
                            var outputPath = executable.OutputMethod.Split('=')[1];

                            outputText.Append(File.ReadAllText(outputPath));
                        }

                        string finalOutput = ToBenchmarkFormat(outputText.ToString());
                        string outputFile = engineName + "_" + test + ".csv";

                        if (File.Exists(outputFile)) File.Delete(outputFile);
                        File.AppendAllText(outputFile, finalOutput);

                        string[] splitResults = finalOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                        var lastResult = splitResults[splitResults.Length - 2];

                        ClearLastLine();
                        Console.ForegroundColor = COLOR_BLUE;
                        Console.Write(executable.Path + " - " + test + " - ");
                        Console.ForegroundColor = COLOR_PINK;
                        Console.WriteLine(lastResult);
                        Console.ForegroundColor = Color.White;
                    }
                }
            }

            PressToExit();

            /*Process app = new Process();

            app.StartInfo.FileName = @"mspaint.exe";
            app.StartInfo.Arguments = TheArgs;
            app.Start();
            app.PriorityClass = ProcessPriorityClass.RealTime;
            */
        }

        static public void ClearLastLine()
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.Write(new string(' ', Console.BufferWidth));
            Console.SetCursorPosition(0, Console.CursorTop - 1);
        }

        static private string ToBenchmarkFormat(string input)
        {
            StringBuilder output = new StringBuilder();

            using (StringReader reader = new StringReader(input))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("bench "))
                    {
                        line = line.Substring(6);
                        line = line.Trim('\r', '\n');
                        output.Append(line + Environment.NewLine);
                    }
                }
            }

            return output.ToString();
        }

        static private void PressToExit()
        {
            Console.ForegroundColor = Color.White;
            Console.WriteLine("Press any key to exit");
            Console.ReadKey(true);
            Environment.Exit(1);
        }

        static private bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}
