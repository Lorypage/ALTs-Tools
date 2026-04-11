using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RefreshToAccess2
{
    public class Helper
    {
        public static string tmpFileName="";
        public static bool PreventWindowRepeat(object obj)
        {
            //if (obj is Window window)
            //{
            //    Type windowType = window.GetType();
            //    bool windowExisted = Application.Current.Windows.OfType<Window>().Any(w => w.GetType() == windowType && w != window);
            //    if (windowExisted)
            //    {
            //        var existedWindow = Application.Current.Windows.OfType<Window>().First();
            //        existedWindow.Topmost= true;
            //        existedWindow.Topmost=false;
            //    }
            //    return true;
            //}
            return false;
        }

        public static void ExtractInjectionDll()
        {
            try
            {
                Random random = new Random();
                tmpFileName = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp\\tmp"+random.NextInt64(8964896489648964).ToString())+".dll");
                using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("RefreshToAccess2.TokenSwapper.dll"))
                {
                    using (var file = new FileStream(tmpFileName, FileMode.Create, FileAccess.Write))
                    {
                        resource.CopyTo(file);
                        file.Close();
                    }
                }
            }catch (Exception ex) { PopException(ex); }
        }

        public static void ExtractFileFromResource(string resourceName,string fileName)
        {
            try
            {
                Random random = new Random();
                using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    using (var file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                    {
                        resource.CopyTo(file);
                    }
                }
            }
            catch (Exception ex) { PopException(ex); }
        }
        public static unsafe ulong GetUnixTimeNative()
        {
            ulong* t = (ulong*)0x7ffe0014;
            return (*t - 0x019DB1DED53E8000) / 10000000;
        }

        public static Process[] GetJavaProcesses()
        {
            Process[] javaProcess = Process.GetProcessesByName("java");
            Process[] javawProcess = Process.GetProcessesByName("javaw");
            return javaProcess.Concat(javawProcess).ToArray();
        }
        public static string GetCommandLine(Process process)
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
            using (ManagementObjectCollection objects = searcher.Get())
            {
                return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
            }
        }

        public static void PopException(Exception exception)
        {
            MessageBox.Show("An exception occurred:\nMessage: "+exception.Message+"\n\nSource: "+exception.Source+"\n\nStackTrace: "+exception.StackTrace+"\n\nInnerException: "+exception.InnerException+"\n\nTargetSite: "+exception.TargetSite);
        }
    }
}
