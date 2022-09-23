using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace Vidian.Modules
{
    class ServiceOptions
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

        [Option("revert", Required = false, HelpText = "Put back the original binpath of the supplied service after execution")]
        public bool revert { get; set; }
    }

    class ServiceTriager
    {
        public static ServiceOptions opts = new ServiceOptions();
        public static void services(string[] args) 
        {
            var parsedResult = Parser.Default.ParseArguments<ServiceOptions>(args.Skip(2)).WithParsed(parser => opts = parser);

            switch (args[1])
            {
                case "binpath":
                    Utils.print.white("[*] Using binpath module");
                    if (opts.name != null)
                    {
                        Utils.print.white("[*] provided service name : " + opts.name);
                        if (opts.binpath != null)
                        {
                            Utils.print.white("[*] provided binpath : " + opts.binpath);
                            Services.Services.modifyService(opts.name, opts.binpath, opts.execute, opts.revert);
                        }
                        else 
                        {
                            Utils.print.red("[-] Provide a binpath, use --help to show options");
                        }
                    }
                    else
                    {
                        Utils.print.red("[-] Provide a service name, use --help to show options");
                    }
                    break;
                case "unquoted":
                    Utils.print.white("[*] Using unquoted module\n");
                    Utils.print.red("NOT IMPLEMENTED YET\n");
                    break;
                default:
                    Utils.print.red("[-] Invalid args");
                    Utils.print.white(@"The available options are:
                    binpath : will modify a provided service's binpath
                    unoquted : will exploit unquoted for provided service
                    ");
                    break;
            }

        }
    }
}
