using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Runtime.InteropServices;
using CommandLine;
using Microsoft.Win32;


namespace Vidian
{
    class Options
    {
        [Option("modify", Required = false, HelpText = "Modify the supplied service")]
        public bool modify { get; set; }

        [Option("list", Required = false, HelpText = "List all services")]
        public bool list { get; set; }

        [Option("execute", Required = false, HelpText = "Domain to connect to")]
        public bool execute { get; set; }

        [Option("name", Required = false, HelpText = "The name of the service to interact with")]
        public string name { get; set; }

        [Option("binpath", Required = false, HelpText = "Modify the binpath of the supplied service")]
        public string binpath { get; set; }

        [Option("original", Required = false, HelpText = "Put back the original binpath of the supplied service after execution")]
        public bool original { get; set; }
    }

    static class ServiceControllerExtension
    {
        public static string GetImagePath(this ServiceController service)
        {
            return Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\" + service.ServiceName, "ImagePath", string.Empty).ToString();
        }
    }
    class Services
    {
        [DllImport("Advapi32.dll", EntryPoint = "ChangeServiceConfigW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ChangeServiceConfig(SafeHandle hService,
            int dwServiceType,
            int dwStartType,
            int dwErrorControl,
            [In] string lpBinaryPathName,
            [In] string lpLoadOrderGroup,
            IntPtr lpdwTagId,
            [In] string lpDependencies,
            [In] string lpServiceStartName,
            [In] string lpPassWord,
            [In] string lpDisplayName
        );

        public static Options opts = new Options();

        public static void services(string[] args)
        {
            try
            {
                var parsedResult = Parser.Default.ParseArguments<Options>(args.Skip(1)).WithParsed(parser => opts = parser);
                if (args.Length == 0)
                {
                    Utils.print.red("[-] Use --help to show options");
                    return;
                }
                
                if (opts.list)
                {
                    Utils.print.blue("\t[*] Discovered services : ");

                    ServiceController[] serviceList = ServiceController.GetServices();

                    foreach (ServiceController service in serviceList)
                    {
                        Utils.print.white("\tName : " + service.ServiceName);
                        Utils.print.white("\tStatus: " + service.Status);
                    }
                }
                // SPN Discvory
                else if (opts.modify)
                {
                    string originalBinPath;
                    /*Utils.print.blue("[*] Modifying a service");*/
                    if (!string.IsNullOrEmpty(opts.name))
                    {
                        Utils.print.blue("\t[*] Modifying the following service : " + opts.name);
                        ServiceController service = new ServiceController(opts.name);
                        SafeHandle serviceHandle = service.ServiceHandle;
                        if (!string.IsNullOrEmpty(opts.binpath))
                        {
                            bool modifyResult;
                            originalBinPath = service.GetImagePath();
                            if (modifyResult = ChangeServiceConfig(serviceHandle, -1, -1, -1, opts.binpath, null, IntPtr.Zero, null, null, null, null))
                            {
                                Utils.print.white("\t[*] Original binpath : " + originalBinPath);
                                Utils.print.green("\t[+] Modified the following service : " + opts.name);
                            }
                            else
                            {
                                Utils.print.red("\t[-] Could not modify the service : " + opts.name);
                                Utils.print.red("\tError : " + Marshal.GetLastWin32Error().ToString());
                            }
                            if (opts.execute)
                            {
                                try
                                {
                                    Utils.print.blue("\t[*] Executing service after modification : " + opts.name);
                                    service.Start();
                                    service.WaitForStatus(ServiceControllerStatus.Running);
                                    Utils.print.blue("\t[+] Started service " + opts.name + " : " + service.Status.ToString());
                                }
                                catch (InvalidOperationException)
                                {
                                    Utils.print.red("\t[-] Could not start service : " + opts.name);
                                }
                            }
                            else
                            {
                                Utils.print.blue("\t[!] Service : " + opts.name + " will not be started, pass --execute after modification");
                            }
                            if (opts.original)
                            {
                                string currentBinPath = service.GetImagePath();
                                Utils.print.white("\t[*] Current binpath : " + currentBinPath);
                                if (modifyResult = ChangeServiceConfig(serviceHandle, -1, -1, -1, originalBinPath, null, IntPtr.Zero, null, null, null, null))
                                {
                                    Utils.print.green("\t[+] Original binpath was put back to service : " + opts.name);
                                }
                                else
                                {
                                    Utils.print.red("\t[-] Could not modify the service : " + opts.name);
                                    Utils.print.red("\tError : " + Marshal.GetLastWin32Error().ToString());
                                }
                            }
                            else 
                            {
                                Utils.print.blue("\t[!] Service : " + opts.name + "'s original binpath will not be put back, pass --original after modification and execution");
                            }
                        }
                        else
                        {
                            Utils.print.red("[-] Provide the binpath you want to use");
                        }
                    }
                    else
                    {
                        Utils.print.red("[-] Provide the name of the service to modify");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}