﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using ReactiveUI;
using RPAK2L.Program.ErrorReporter;
using RPAK2L.Program.Headers;
using RPAK2L.Program.ViewModels.FileView.Views;
using RPAK2L.Program.Views.SubMenus;

namespace RPAK2L.Program
{
    
    
    class ICrashLifetime : 
        IControlledApplicationLifetime,
        IApplicationLifetime
    {
        public Window MainWindow;
        public void Shutdown(int exitCode = 0)
        {
            throw new NotImplementedException();
        }

        public event EventHandler<ControlledApplicationLifetimeStartupEventArgs>? Startup;
        public event EventHandler<ControlledApplicationLifetimeExitEventArgs>? Exit;
    }
    
    public class Program
    {
        public static Window AppMainWindow;
        public static HeaderInterface Headers;
        private static AppBuilder _builderInstance;
        public static bool Updating = false;
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        
        /// <summary>
        /// Main program function
        /// </summary>
        /// <param name="args"></param>
        /// <returns>"ok" if normally run, the log path if crashed, the install folder if updating</returns>
        public static string Main(string[] args)
        {
            Directory.CreateDirectory("./Logs");
            bool foundAvailableLog = false;
            string logPath = "";
            int tmp = 0;
            while (!foundAvailableLog)
            {
                string suffix = (tmp > 0) ? $"_{tmp.ToString().PadLeft(2,'0')}" : "";
                string path = Path.Combine(Environment.CurrentDirectory, "Logs",$"log_{DateTime.Now.ToString("yyyy.MM.dd.HH")}{suffix}.log");
                if (!File.Exists(path))
                {
                    logPath = path;
                    foundAvailableLog = true;
                }
                tmp++;
            }
            Settings.Init(Path.Combine(Environment.CurrentDirectory, "settings.ini"));
            Settings.Load();
            Logger.Log = new Logger(logPath);
            Headers = new HeaderInterface();
            if (OperatingSystem.IsWindows())
            {
                if (!Debugger.IsAttached) //reponse to https://github.com/headassbtw/rpak2l/issues/1
                {
                    //when compiled on linux, C# apps always show a console on windows. i don't want that.
                    #pragma warning disable CA1416 //this is under a function that only runs on windows
                    Console.SetWindowSize(1,1);
                    #pragma warning restore CA1416
                    Console.Title = "Joseph Mother";
                    var handle = GetConsoleWindow();
                    ShowWindow(handle, 0);
                }
            }
            try
            {
                RxApp.DefaultExceptionHandler = new DontCrashPlease();
                _builderInstance = BuildAvaloniaApp();
                
                _builderInstance.StartWithClassicDesktopLifetime(args);
            }
            catch (Exception exc)
            {
                return logPath;
                //AppMainWindow.Close();

                Logger.Log.Error("Fatal Error:");
                Logger.Log.Error(exc);
                string reporterPath = Path.Combine(Environment.CurrentDirectory, "RPAK2L.Reporter");
                Process reporterProcess = new Process();
                reporterProcess.StartInfo.Arguments = $"-path {logPath} -p {Assembly.GetExecutingAssembly().GetName().Name}";
                if(OperatingSystem.IsLinux())
                    reporterProcess.StartInfo.FileName = reporterPath;
                if(OperatingSystem.IsWindows())
                    reporterProcess.StartInfo.FileName = reporterPath + ".exe";
                Logger.Log.Info($"Calling {reporterPath} with args: [{reporterProcess.StartInfo.Arguments}]");
                Logger.Log.Info("Exiting");
                Logger.Log.Close();
                reporterProcess.Start();
                reporterProcess.WaitForExit();
            }

            if (Updating) return Environment.CurrentDirectory;
            return "ok";
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();
    }
}
