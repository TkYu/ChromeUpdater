using System;
using CommonUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void HaveInternet()
        {
            Assert.IsTrue(Utils.Ping("www.baidu.com").Result);
        }

        [TestMethod]
        public void CanOpenGoogle()
        {
            Assert.IsTrue(Utils.CanUseGoogle(2000).Result);
        }

        [TestMethod]
        public void GoogleInstalled()
        {
            Console.WriteLine(Utils.TryGetCurrChromeExePath());
            Console.WriteLine(Utils.IsX64Image(Utils.TryGetCurrChromeExePath())?"Is X64":"Is X86");
            StringAssert.Contains(Utils.TryGetCurrChromeExePath(),"chrome.exe");
        }
    }
}
