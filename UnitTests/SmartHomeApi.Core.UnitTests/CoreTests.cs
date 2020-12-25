using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Common.Utils;
using NUnit.Framework;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.Core.Interfaces.Extensions;
using SmartHomeApi.Core.ItemHelpers;
using SmartHomeApi.ItemUtils;

namespace SmartHomeApi.Core.UnitTests
{
    class CoreTests
    {
        [Test]
        public void AverageValuesHelper1()
        {
            AverageValuesHelper helper = new AverageValuesHelper(10);

            double value = 2;
            value = helper.GetAverageValue(value);
            Assert.AreEqual(2, value);

            value = 3;
            value = helper.GetAverageValue(value);
            Assert.AreEqual(2.5, value);

            //Assert.Pass();
        }

        [Test]
        public void StateTypeConverterTest()
        {
            var res1 = TypeHelper.GetValue<int?>(7);
            var res2 = TypeHelper.GetValue<int?>(null);

            try
            {
                var res3 = TypeHelper.GetValue<int?>("test");
            }
            catch (InvalidCastException)
            {
            }
            catch (FormatException)
            {
            }

            try
            {
                var res4 = TypeHelper.GetValue<int?>(7.0);
            }
            catch (InvalidCastException)
            {
            }

            var res5 = TypeHelper.GetValue<int>(7);
            var res6 = TypeHelper.GetValue<string>(null);
            var res7 = TypeHelper.GetValue<string>("test");

            try
            {
                var res8 = TypeHelper.GetValue<int>(null);
            }
            catch (NullReferenceException)
            {
            }
            catch (InvalidCastException)
            {
            }

            var res9 = TypeHelper.GetValue(null, 7);

            var testInt = 23;
            var testStr = "teststr";
            var testObj = new TestClass { TestInt = testInt, TestStr = testStr };

            var serializer = new NewtonsoftJsonSerializer();
            var ser = serializer.Serialize(testObj);

            TestClass res10 = TypeHelper.GetValue<TestClass>(ser);
            Assert.AreEqual(testInt, res10.TestInt);
            Assert.AreEqual(testStr, res10.TestStr);

            TestClass res11 = TypeHelper.GetValue(null, new TestClass { TestInt = 10, TestStr = "test" });
            Assert.AreEqual(10, res11.TestInt);
            Assert.AreEqual("test", res11.TestStr);

            TestClass res12 = TypeHelper.GetValue<TestClass>(null);
            Assert.IsNull(res12);

            TestClass res13 = TypeHelper.GetValue<TestClass>(null, null);
            Assert.IsNull(res13);

            long test = 7;
            var res14 = TypeHelper.GetValue<int>(test);
            Assert.AreEqual(7, res14);

            var res15 = TypeHelper.GetValue<int?>(test);
            Assert.AreEqual(7, res15);
        }

        [Test]
        public void GetAsObjectTest()
        {
            var culture = CultureInfo.GetCultureInfo("ru-RU");
            var res1 = "test".GetAsObject(ValueDataType.String, culture);
            Assert.AreEqual("test", res1);

            culture = CultureInfo.GetCultureInfo("en-EN");
            var res101 = "test".GetAsObject(ValueDataType.String, culture);
            Assert.AreEqual("test", res101);

            GetAsObjectTest_Integer();
            GetAsObjectTest_Double();
            GetAsObjectTest_Decimal();
            GetAsObjectTest_Boolean();
            GetAsObjectTest_DateTime();
            GetAsObjectTest_TimeSpan();
        }

        private void GetAsObjectTest_Integer()
        {
            var culture = CultureInfo.GetCultureInfo("ru-RU");

            var res2 = "10".GetAsObject(ValueDataType.Integer, culture);
            Assert.AreEqual(10, res2);

            var res3 = "10,0".GetAsObject(ValueDataType.Integer, culture);
            Assert.AreEqual(10, res3);

            try
            {
                var res4 = "10,5".GetAsObject(ValueDataType.Integer, culture);
                Assert.Fail();
            }
            catch (Exception e)
            {
            }

            try
            {
                var res5 = "10.0".GetAsObject(ValueDataType.Integer, culture);
                Assert.Fail();
            }
            catch (Exception e)
            {
            }

            culture = CultureInfo.GetCultureInfo("en-EN");

            var res102 = "10".GetAsObject(ValueDataType.Integer, culture);
            Assert.AreEqual(10, res102);

            var res103 = "10.0".GetAsObject(ValueDataType.Integer, culture);
            Assert.AreEqual(10, res103);

            try
            {
                var res104 = "10.5".GetAsObject(ValueDataType.Integer, culture);
                Assert.Fail();
            }
            catch (Exception e)
            {
            }

            try
            {
                var res105 = "10,0".GetAsObject(ValueDataType.Integer, culture);
                Assert.AreEqual(100, res105);
            }
            catch (Exception e)
            {
            }
        }

        private void GetAsObjectTest_Double()
        {
            var culture = CultureInfo.GetCultureInfo("ru-RU");

            var res1 = "10".GetAsObject(ValueDataType.Double, culture);
            Assert.AreEqual(10d, res1);

            var res2 = "10,0".GetAsObject(ValueDataType.Double, culture);
            Assert.AreEqual(10d, res2);

            var res3 = "10,5".GetAsObject(ValueDataType.Double, culture);
            Assert.AreEqual(10.5d, res3);

            try
            {
                var res4 = "10.5".GetAsObject(ValueDataType.Double, culture);
                Assert.Fail();
            }
            catch (Exception e)
            {
            }

            culture = CultureInfo.GetCultureInfo("en-EN");

            var res101 = "10".GetAsObject(ValueDataType.Double, culture);
            Assert.AreEqual(10d, res1);

            var res102 = "10.0".GetAsObject(ValueDataType.Double, culture);
            Assert.AreEqual(10d, res2);

            var res103 = "10.5".GetAsObject(ValueDataType.Double, culture);
            Assert.AreEqual(10.5d, res103);

            try
            {
                var res104 = "10,5".GetAsObject(ValueDataType.Double, culture);
                Assert.AreEqual(105d, res104);
            }
            catch (Exception e)
            {
            }
        }

        private void GetAsObjectTest_Decimal()
        {
            var culture = CultureInfo.GetCultureInfo("ru-RU");

            var res1 = "10".GetAsObject(ValueDataType.Decimal, culture);
            Assert.AreEqual(10m, res1);

            var res2 = "10,0".GetAsObject(ValueDataType.Decimal, culture);
            Assert.AreEqual(10m, res2);

            var res3 = "10,5".GetAsObject(ValueDataType.Decimal, culture);
            Assert.AreEqual(10.5m, res3);

            try
            {
                var res4 = "10.5".GetAsObject(ValueDataType.Decimal, culture);
                Assert.Fail();
            }
            catch (Exception e)
            {
            }

            culture = CultureInfo.GetCultureInfo("en-EN");

            var res101 = "10".GetAsObject(ValueDataType.Decimal, culture);
            Assert.AreEqual(10m, res1);

            var res102 = "10.0".GetAsObject(ValueDataType.Decimal, culture);
            Assert.AreEqual(10m, res2);

            var res103 = "10.5".GetAsObject(ValueDataType.Decimal, culture);
            Assert.AreEqual(10.5m, res103);

            try
            {
                var res104 = "10,5".GetAsObject(ValueDataType.Decimal, culture);
                Assert.AreEqual(105m, res104);
            }
            catch (Exception e)
            {
            }
        }

        private void GetAsObjectTest_Boolean()
        {
            var culture = CultureInfo.GetCultureInfo("ru-RU");

            var res1 = "true".GetAsObject(ValueDataType.Boolean, culture);
            Assert.AreEqual(true, res1);

            var res2 = "false".GetAsObject(ValueDataType.Boolean, culture);
            Assert.AreEqual(false, res2);

            var res3 = "True".GetAsObject(ValueDataType.Boolean, culture);
            Assert.AreEqual(true, res3);

            var res4 = "False".GetAsObject(ValueDataType.Boolean, culture);
            Assert.AreEqual(false, res4);

            var res5 = "1".GetAsObject(ValueDataType.Boolean, culture);
            Assert.AreEqual(true, res5);

            var res6 = "0".GetAsObject(ValueDataType.Boolean, culture);
            Assert.AreEqual(false, res6);

            culture = CultureInfo.GetCultureInfo("en-EN");

            var res101 = "true".GetAsObject(ValueDataType.Boolean, culture);
            Assert.AreEqual(true, res101);

            var res102 = "false".GetAsObject(ValueDataType.Boolean, culture);
            Assert.AreEqual(false, res102);

            var res103 = "True".GetAsObject(ValueDataType.Boolean, culture);
            Assert.AreEqual(true, res103);

            var res104 = "False".GetAsObject(ValueDataType.Boolean, culture);
            Assert.AreEqual(false, res104);

            var res105 = "1".GetAsObject(ValueDataType.Boolean, culture);
            Assert.AreEqual(true, res105);

            var res106 = "0".GetAsObject(ValueDataType.Boolean, culture);
            Assert.AreEqual(false, res106);
        }

        private void GetAsObjectTest_DateTime()
        {
            var dt1 = new DateTime(2020, 11, 27, 2, 35, 44);

            var culture = CultureInfo.GetCultureInfo("ru-RU");

            var res1 = "27.11.2020 2:35:44".GetAsObject(ValueDataType.DateTime, culture);
            Assert.AreEqual(dt1, res1);

            try
            {
                var res2 = "11/27/2020 2:35:44 AM".GetAsObject(ValueDataType.DateTime, culture);
                Assert.Fail();
            }
            catch (Exception e)
            {
            }

            culture = CultureInfo.GetCultureInfo("en-EN");

            var res101 = "11/27/2020 2:35:44 AM".GetAsObject(ValueDataType.DateTime, culture);
            Assert.AreEqual(dt1, res101);

            try
            {
                var res102 = "27.11.2020 2:35:44".GetAsObject(ValueDataType.DateTime, culture);
                Assert.Fail();
            }
            catch (Exception e)
            {
            }
        }

        private void GetAsObjectTest_TimeSpan()
        {
            var t1 = new TimeSpan(2, 35, 44);

            var culture = CultureInfo.GetCultureInfo("ru-RU");

            var res1 = "2:35:44".GetAsObject(ValueDataType.TimeSpan, culture);
            Assert.AreEqual(t1, res1);

            try
            {
                var res2 = "2:35:44 AM".GetAsObject(ValueDataType.TimeSpan, culture);
                Assert.Fail();
            }
            catch (Exception e)
            {
            }

            try
            {
                var res3 = "27.11.2020 2:35:44".GetAsObject(ValueDataType.TimeSpan, culture);
                Assert.Fail();
            }
            catch (Exception e)
            {
            }


            culture = CultureInfo.GetCultureInfo("en-EN");

            var res101 = "2:35:44".GetAsObject(ValueDataType.TimeSpan, culture);
            Assert.AreEqual(t1, res101);

            try
            {
                var res102 = "2:35:44 AM".GetAsObject(ValueDataType.TimeSpan, culture);
                Assert.Fail();
            }
            catch (Exception e)
            {
            }
        }

        [Test]
        public void SerializationTest()
        {
            var dtNow = DateTime.Now;
            var obj = new DoubleTest { Value = 10, Date = dtNow };

            var culture = CultureInfo.GetCultureInfo("ru-RU");

            Thread.CurrentThread.CurrentCulture = culture;
            var dt = DateTime.Now.ToLongTimeString();

            var serializer = new NewtonsoftJsonSerializer();
            var serialized = serializer.Serialize(obj);

            culture = CultureInfo.GetCultureInfo("en-EN");
            Thread.CurrentThread.CurrentCulture = culture;
            serialized = serializer.Serialize(obj);

            var res1 = bool.Parse("true");
            var res2 = bool.Parse("True");

            var res3 = bool.Parse("false");
            var res4 = bool.Parse("False");

            var timeSpanMs = DateTime.Now.TimeOfDay;
            var time = new TimeSpan(timeSpanMs.Hours, timeSpanMs.Minutes, timeSpanMs.Seconds);
            var timeFull = new TimeSpan(3, time.Hours, time.Minutes, time.Seconds, 123);
            var dict = new Dictionary<string, object>();

            dict.Add("String", "Indoor");
            dict.Add("StringNull", null);
            dict.Add("Integer", 3);
            dict.Add("Double", 1.5d);
            dict.Add("Decimal", 2.5m);
            dict.Add("Boolean", true);
            dict.Add("DateTime", dtNow);
            dict.Add("TimeSpan", time);
            dict.Add("TimeSpanMs", timeSpanMs);
            dict.Add("TimeSpanFull", timeFull);

            serialized = serializer.Serialize(dict);

            var desDict = serializer.Deserialize<Dictionary<string, object>>(serialized);

            Assert.AreEqual("Indoor", desDict["String"]);
            Assert.AreEqual(null, desDict["StringNull"]);
            Assert.AreEqual(3, (int)(long)desDict["Integer"]);
            Assert.AreEqual(1.5d, (double)desDict["Double"]);
            Assert.AreEqual(2.5m, (decimal)(double)desDict["Decimal"]);
            Assert.AreEqual(true, (bool)desDict["Boolean"]);
            Assert.AreEqual(dtNow, (DateTime)desDict["DateTime"]);
            Assert.AreEqual(time, (TimeSpan)desDict["TimeSpan"]);
            Assert.AreEqual(timeSpanMs, (TimeSpan)desDict["TimeSpanMs"]);
            Assert.AreEqual(timeFull, (TimeSpan)desDict["TimeSpanFull"]);

            serialized = serializer.Serialize(obj);
            var doubleTest = serializer.Deserialize<DoubleTest>(serialized);
            Assert.IsNotNull(doubleTest);
            Assert.AreEqual(10, doubleTest.Value);
            Assert.AreEqual(dtNow, doubleTest.Date);
        }

        private class TestClass
        {
            public string TestStr { get; set; }
            public int TestInt { get; set; }
        }

        private class DoubleTest
        {
            public double Value { get; set; }
            public DateTime Date { get; set; }
        }
    }
}