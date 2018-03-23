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
            foreach (string executable in Properties.Settings.Default.Executables)
            {
                if (!File.Exists(executable))
                {
                    Console.WriteLine(executable + " (not found!)");

                    erroredExecutables = true;
                }
                else
                {
                    Console.WriteLine(executable);
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
            foreach (string test in Properties.Settings.Default.Tests)
            {
                Console.WriteLine(test);
            }

            Console.ForegroundColor = COLOR_YELLOW;
            Console.WriteLine("Executing automated tests...");
            //while (Console.ReadKey(true).Key != ConsoleKey.Enter) ;
           
            foreach (string executable in Properties.Settings.Default.Executables)
            {
                var engineName = executable.Split('\\')[0];

                Console.ForegroundColor = Color.White;

                foreach (string test in Properties.Settings.Default.Tests)
                {
                    Console.WriteLine(executable + " - " + test);

                    using (var runningTest = new Process())
                    {
                        var outputText = new StringBuilder();

                        runningTest.StartInfo.FileName = Path.Combine(Environment.CurrentDirectory, executable);
                        runningTest.StartInfo.Arguments = "-logFile -test=" + test;
                        runningTest.StartInfo.RedirectStandardOutput = true;
                        runningTest.StartInfo.UseShellExecute = false;
                        runningTest.OutputDataReceived += (_, dataEvent) => outputText.Append(dataEvent.Data);
                        runningTest.Start();
                        runningTest.PriorityClass = ProcessPriorityClass.RealTime;
                        runningTest.BeginOutputReadLine();
                        runningTest.WaitForExit();

                        File.AppendAllText(engineName + "_" + test + ".csv", outputText.ToString());
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
