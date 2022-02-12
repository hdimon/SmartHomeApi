using System;
using System.Collections.Generic;
using System.Dynamic;
using NUnit.Framework;

namespace SmartHomeApi.Core.UnitTests;

internal class DynamicToObjectMapperTests
{
    [Test]
    public void PrimitivesTest()
    {
        var mapper = new DynamicToObjectMapper();

        int varInt = 20;
        var resultInt = (int)mapper.Map(varInt, typeof(int));
        Assert.AreEqual(20, resultInt);

        long varLong = 40;
        var resultLong = mapper.Map(varLong, typeof(int));
        Assert.AreEqual(typeof(int), resultLong.GetType());
        Assert.AreEqual(40, resultLong);

        dynamic data = new ExpandoObject();
        data.ObjectTypeProperty = "Test";
        data.ValueInt = 15;
        data.ValueInt64AsInt32 = (long)16;
        data.ValueInt64 = (long)17;
        data.ValueDecimal = 18m;
        data.ValueDouble = 19d;
        data.ValueBool = true;
        data.ValueBoolNull = null;
        data.ValueStr = "test";
        data.ValueDateTime = new DateTime(2022, 2, 2, 15, 11, 12);

        var result = (PrimitivesTestObject)mapper.Map(data, typeof(PrimitivesTestObject));

        Assert.AreEqual(15, result.ValueInt);
        Assert.AreEqual(16, result.ValueInt64AsInt32);
        Assert.AreEqual(17, result.ValueInt64);
        Assert.AreEqual(18m, result.ValueDecimal);
        Assert.AreEqual(19d, result.ValueDouble);
        Assert.AreEqual(true, result.ValueBool);
        Assert.IsNull(result.ValueBoolNull);
        Assert.AreEqual("test", result.ValueStr);
        Assert.AreEqual(new DateTime(2022, 2, 2, 15, 11, 12), result.ValueDateTime);
    }

    [Test]
    public void PrimitivesListTest()
    {
        var mapper = new DynamicToObjectMapper();

        var list = new List<long>();

        var result = (List<long>)mapper.Map(list, typeof(List<long>));
        Assert.AreEqual(0, result.Count);

        list = new List<long>();
        list.Add(15);
        list.Add(23);

        result = (List<long>)mapper.Map(list, typeof(List<long>));

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(15, result[0]);
        Assert.AreEqual(23, result[1]);

        var listObj = new List<object>();
        listObj.Add(15);
        listObj.Add("Test");

        var resultObj = (List<object>)mapper.Map(listObj, typeof(List<object>));

        Assert.AreEqual(2, resultObj.Count);
        Assert.AreEqual(15, resultObj[0]);
        Assert.AreEqual("Test", resultObj[1]);
    }

    [Test]
    public void PrimitivesListOfListsTest()
    {
        var mapper = new DynamicToObjectMapper();

        var list = new List<List<long>>();
        list.Add(new List<long> { 15, 23 });

        var result = (List<List<long>>)mapper.Map(list, typeof(List<List<long>>));
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(2, result[0].Count);
        Assert.AreEqual(15, result[0][0]);
        Assert.AreEqual(23, result[0][1]);
    }

    [Test]
    public void PrimitivesDictionaryOfListsTest()
    {
        var mapper = new DynamicToObjectMapper();

        var dict = new Dictionary<string, List<long>>();
        dict.Add("TestKey", new List<long> { 15 });

        var result = (Dictionary<string, List<long>>)mapper.Map(dict, typeof(Dictionary<string, List<long>>));
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result.ContainsKey("TestKey"));
        Assert.AreEqual(15, result["TestKey"][0]);
    }

    [Test]
    public void ObjectsListTest()
    {
        var mapper = new DynamicToObjectMapper();

        var list = new List<ExpandoObject>();
        dynamic obj = new ExpandoObject();
        obj.Text = "Test";
        obj.Number = 23;
        list.Add(obj);

        var result = (List<SimpleObject>)mapper.Map(list, typeof(List<SimpleObject>));

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Test", result[0].Text);
        Assert.AreEqual(23, result[0].Number);

        obj.Number = 24;

        Assert.AreEqual(23, result[0].Number);
    }

    [Test]
    public void ObjectWithPrimitivesListTest()
    {
        var mapper = new DynamicToObjectMapper();

        dynamic data = new ExpandoObject();
        data.ListInt = new List<object>();
        data.ListInt.Add((long)23);
        data.ListInt.Add((long)35);
        data.ListInt.Add(47);

        var result = (PrimitivesListObject)mapper.Map(data, typeof(PrimitivesListObject));

        Assert.AreEqual(3, result.ListInt.Count);
        Assert.AreEqual(23, result.ListInt[0]);
        Assert.AreEqual(35, result.ListInt[1]);
        Assert.AreEqual(47, result.ListInt[2]);

        //The same but for case when destination list is IList<> instead of List<>
        var iResult = (PrimitivesIListObject)mapper.Map(data, typeof(PrimitivesIListObject));

        Assert.AreEqual(3, iResult.ListInt.Count);
        Assert.AreEqual(23, iResult.ListInt[0]);
        Assert.AreEqual(35, iResult.ListInt[1]);
        Assert.AreEqual(47, iResult.ListInt[2]);
    }

    [Test]
    public void ObjectWithPrimitivesListIntTest()
    {
        var mapper = new DynamicToObjectMapper();

        dynamic data = new ExpandoObject();
        data.ListInt = new List<long>();
        data.ListInt.Add((long)23);
        data.ListInt.Add((long)35);
        data.ListInt.Add(47);

        var result = (PrimitivesListIntObject)mapper.Map(data, typeof(PrimitivesListIntObject));

        Assert.AreEqual(3, result.ListInt.Count);
        Assert.AreEqual(23, result.ListInt[0]);
        Assert.AreEqual(35, result.ListInt[1]);
        Assert.AreEqual(47, result.ListInt[2]);
    }

    [Test]
    public void ObjectWithListOfNestedObjectsTest()
    {
        var mapper = new DynamicToObjectMapper();

        dynamic data = new ExpandoObject();
        data.NestedList = new List<ExpandoObject>();
        dynamic obj1 = new ExpandoObject();
        obj1.Text = "Text1";
        obj1.Number = 11;
        data.NestedList.Add(obj1);
        dynamic obj2 = new ExpandoObject();
        obj2.Text = "Text2";
        obj2.Number = 22;
        data.NestedList.Add(obj2);

        var result = (ListOfNestedObjects)mapper.Map(data, typeof(ListOfNestedObjects));

        Assert.AreEqual(2, result.NestedList.Count);
        Assert.AreEqual("Text1", result.NestedList[0].Text);
        Assert.AreEqual(11, result.NestedList[0].Number);
        Assert.AreEqual("Text2", result.NestedList[1].Text);
        Assert.AreEqual(22, result.NestedList[1].Number);
    }

    [Test]
    public void PrimitivesDictionaryObjectTest()
    {
        var mapper = new DynamicToObjectMapper();

        dynamic data = new ExpandoObject();
        data.DictionaryObj = new ExpandoObject();
        data.DictionaryObj.Int = 23;
        data.DictionaryObj.Long = (long)34;
        data.DictionaryObj.Str = "Test";
        data.DictionaryObj.Decimal = 123.46m;
        data.DictionaryObj.Double = 1230.046d;
        data.DictionaryObj.ValueBool = true;
        data.DictionaryObj.ValueDateTime = new DateTime(2022, 2, 2, 15, 11, 12);

        var result = (PrimitivesDictionaryObject)mapper.Map(data, typeof(PrimitivesDictionaryObject));

        Assert.AreEqual(7, result.DictionaryObj.Count);
        Assert.AreEqual(23, result.DictionaryObj["Int"]);
        Assert.AreEqual((long)34, result.DictionaryObj["Long"]);
        Assert.AreEqual("Test", result.DictionaryObj["Str"]);
        Assert.AreEqual(123.46m, result.DictionaryObj["Decimal"]);
        Assert.AreEqual(1230.046d, result.DictionaryObj["Double"]);
        Assert.AreEqual(true, result.DictionaryObj["ValueBool"]);
        Assert.AreEqual(new DateTime(2022, 2, 2, 15, 11, 12), result.DictionaryObj["ValueDateTime"]);

        data = new ExpandoObject();
        data.DictionaryObj = new Dictionary<string, object>();
        data.DictionaryObj["Int"] = 23;
        data.DictionaryObj["Long"] = (long)34;
        data.DictionaryObj["Str"] = "Test";
        data.DictionaryObj["Decimal"] = 123.46m;
        data.DictionaryObj["Double"] = 1230.046d;
        data.DictionaryObj["ValueBool"] = true;
        data.DictionaryObj["ValueDateTime"] = new DateTime(2022, 2, 2, 15, 11, 12);
        result = (PrimitivesDictionaryObject)mapper.Map(data, typeof(PrimitivesDictionaryObject));

        Assert.AreEqual(7, result.DictionaryObj.Count);
        Assert.AreEqual(23, result.DictionaryObj["Int"]);
        Assert.AreEqual((long)34, result.DictionaryObj["Long"]);
        Assert.AreEqual("Test", result.DictionaryObj["Str"]);
        Assert.AreEqual(123.46m, result.DictionaryObj["Decimal"]);
        Assert.AreEqual(1230.046d, result.DictionaryObj["Double"]);
        Assert.AreEqual(true, result.DictionaryObj["ValueBool"]);
        Assert.AreEqual(new DateTime(2022, 2, 2, 15, 11, 12), result.DictionaryObj["ValueDateTime"]);

        //The same but for case when destination dictionary is IDictionary<> instead of Dictionary<>
        var iResult = (PrimitivesIDictionaryObject)mapper.Map(data, typeof(PrimitivesIDictionaryObject));

        Assert.AreEqual(7, iResult.DictionaryObj.Count);
        Assert.AreEqual(23, iResult.DictionaryObj["Int"]);
        Assert.AreEqual((long)34, iResult.DictionaryObj["Long"]);
        Assert.AreEqual("Test", iResult.DictionaryObj["Str"]);
        Assert.AreEqual(123.46m, iResult.DictionaryObj["Decimal"]);
        Assert.AreEqual(1230.046d, iResult.DictionaryObj["Double"]);
        Assert.AreEqual(true, iResult.DictionaryObj["ValueBool"]);
        Assert.AreEqual(new DateTime(2022, 2, 2, 15, 11, 12), iResult.DictionaryObj["ValueDateTime"]);
    }

    [Test]
    public void DictionaryOfNestedObjectsTest()
    {
        var mapper = new DynamicToObjectMapper();

        dynamic data = new ExpandoObject();
        data.NestedDict = new Dictionary<string, ExpandoObject>();
        dynamic obj1 = new ExpandoObject();
        obj1.Text = "Text1";
        obj1.Number = 11;
        data.NestedDict.Add("Key1", obj1);
        dynamic obj2 = new ExpandoObject();
        obj2.Text = "Text2";
        obj2.Number = 22;
        data.NestedDict.Add("Key2", obj2);

        var result = (DictionaryOfNestedObjects)mapper.Map(data, typeof(DictionaryOfNestedObjects));

        Assert.AreEqual(2, result.NestedDict.Count);
        Assert.AreEqual("Text1", result.NestedDict["Key1"].Text);
        Assert.AreEqual(11, result.NestedDict["Key1"].Number);
        Assert.AreEqual("Text2", result.NestedDict["Key2"].Text);
        Assert.AreEqual(22, result.NestedDict["Key2"].Number);
    }

    [Test]
    public void NestedObjectTest()
    {
        var mapper = new DynamicToObjectMapper();

        dynamic data = new ExpandoObject();
        data.NestedObj = new ExpandoObject();
        data.NestedObj.Text = "Text1";
        data.NestedObj.Number = 11;

        var result = (ParentObject)mapper.Map(data, typeof(ParentObject));

        Assert.NotNull(result.NestedObj);
        Assert.AreEqual("Text1", result.NestedObj.Text);
        Assert.AreEqual(11, result.NestedObj.Number);
    }

    [Test]
    public void DictionaryTest()
    {
        var mapper = new DynamicToObjectMapper();

        dynamic data = new ExpandoObject();
        data.Text = "Test";
        data.Number = 11;

        var result = (Dictionary<string, object>)mapper.Map(data, typeof(Dictionary<string, object>));

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("Test", result["Text"]);
        Assert.AreEqual(11, result["Number"]);

        data = new ExpandoObject();
        data.Number1 = (long)11;
        data.Number2 = 22;

        var resultInt = (Dictionary<string, int>)mapper.Map(data, typeof(Dictionary<string, int>));

        Assert.AreEqual(2, resultInt.Count);
        Assert.AreEqual(11, resultInt["Number1"]);
        Assert.AreEqual(22, resultInt["Number2"]);
    }

    private class PrimitivesTestObject
    {
        public object ObjectTypeProperty { get; set; }
        public int ValueInt { get; set; }
        /// <summary>
        /// Case when data comes from JSON and JObject.ToObject<ExpandoObject>() makes value as Int64
        /// though destination Class expects Int32
        /// </summary>
        public int ValueInt64AsInt32 { get; set; }
        public long ValueInt64 { get; set; }
        public decimal ValueDecimal { get; set; }
        public double ValueDouble { get; set; }
        public bool ValueBool { get; set; }
        public bool? ValueBoolNull { get; set; }
        public string ValueStr { get; set; }
        public DateTime ValueDateTime { get; set; }
    }

    private class PrimitivesListObject
    {
        public List<object> ListInt { get; set; }
    }

    private class PrimitivesIListObject
    {
        public IList<object> ListInt { get; set; }
    }

    private class ListOfNestedObjects
    {
        public List<SimpleObject> NestedList { get; set; }
    }

    private class PrimitivesListIntObject
    {
        public List<int> ListInt { get; set; }
    }

    private class PrimitivesDictionaryObject
    {
        public Dictionary<string, object> DictionaryObj { get; set; }
    }

    private class PrimitivesIDictionaryObject
    {
        public IDictionary<string, object> DictionaryObj { get; set; }
    }

    private class DictionaryOfNestedObjects
    {
        public Dictionary<string, SimpleObject> NestedDict { get; set; }
    }

    private class ParentObject
    {
        public SimpleObject NestedObj { get; set; }
    }

    private class SimpleObject
    {
        public string Text { get; set; }
        public int Number { get; set; }
    }
}