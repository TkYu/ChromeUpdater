using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonUtils;

namespace UnitTestProject1
{
    class Program
    {
        static Models.CUnit cu = null;
        private static async Task getUpdate(string Branch,bool isX64)
        {
            
            if (await Utils.CanUseGoogle())
            {
                cu = await Utils.GetUpdateFromGoogle(Branch, isX64 ? "X64" : "X86");
            }
            else
            {
                cu = (await Utils.GetUpdateFromShuax()).GetCU(Branch, isX64);
            }
            if (cu != null)
            {
                Console.WriteLine(string.Join(Environment.NewLine,cu.url));
            }

        }
        private static async Task DoWork(string[] args)
        {
            if (!await Utils.Ping("www.baidu.com")) return;
            Console.WriteLine("You have Internet access");
            await getUpdate("Dev", false);
        }

        [STAThread]
        public static void Main(string[] args)
        {
            DoWork(args).GetAwaiter().GetResult();
            Console.ReadLine();
        }
    }
}
