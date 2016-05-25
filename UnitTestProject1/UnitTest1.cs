using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using SchemeOfAlgorithm;
using System.Reflection;
using System.Collections.Generic;



namespace UnitTestProject1
{
    public class One
    {
        public static void someMethod(List<int> list)
        {
            list.Add(1);
        }
        public static void someMethod(ref List<int> list)
        {
            list.Add(2);
        }
    }
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            List<int> somelist = new List<int>();
            One.someMethod(somelist);
            Assert.AreEqual(somelist[0], 1);            
        }
        [TestMethod]
        public void TestMethod2()
        {
            List<int> somelist = new List<int>();
            One.someMethod(ref somelist);
            Assert.AreEqual(somelist[0], 2);
        }
    }
}
    

