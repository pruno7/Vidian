using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vidian
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Utils.print.red("[-] Invalid args");
                Utils.print.white(@"The available options are:
                services : will enable you to interact with services
                schtasks : will enable you to interact with scheduled tasks
                ");
                return;
            }
            var option = args[0];
            switch (option)
            {
                case "services":
                    Services.services(args);
                    break;
                /*case "schtasks":
                    Schtasks.schtasks();
                    break;*/
                default:
                    Utils.print.red("[-] Invalid option");
                    break;
            }
        }
    }
}
