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
        public static void Main(string[] args)
        {
            Console.WriteLine(Utils.Ping("www.baidu.com").Result);
        }
    }
}
