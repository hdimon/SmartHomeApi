using System;
using System.Collections.Generic;
using System.Dynamic;
using NUnit.Framework;

namespace SmartHomeApi.Core.UnitTests;

internal class ObjectToDynamicConverterTests
{
    [Test]
    public void PrimitivesTest()
    {
        var converter = new ObjectToDynamicConverter();

        var result = converter.Convert(15);

        Assert.AreEqual(15, (int)result);

        result = converter.Convert(null);

        Assert.IsNull(result);

        result = converter.Convert(new DateTime(2022, 2, 2, 15, 11, 12));

        Assert.AreEqual(new DateTime(2022, 2, 2, 15, 11, 12), result);

        result = converter.Convert("Test");

        Assert.AreEqual("Test", result);
    }

    [Test]
    public void PrimitivesListTest()
    {
        var converter = new ObjectToDynamicConverter();

        var list = new List<int>();

        var result = converter.Convert(list);
        var resultList = result as List<int>;

        Assert.IsNotNull(resultList);
        Assert.AreEqual(0, resultList.Count);

        list = new List<int>();
        list.Add(15);

        result = converter.Convert(list);

        resultList = result as List<int>;

        Assert.IsNotNull(resultList);
        Assert.AreEqual(1, resultList.Count);
        Assert.AreEqual(15, resultList[0]);

        var listObj = new List<object>();
        listObj.Add(15);
        listObj.Add("Test");

        result = converter.Convert(listObj);

        var resultListObj = result as List<object>;

        Assert.IsNotNull(resultListObj);
        Assert.AreEqual(2, resultListObj.Count);
        Assert.AreEqual(15, resultListObj[0]);
        Assert.AreEqual("Test", resultListObj[1]);
    }

    [Test]
    public void ObjectsListTest()
    {
        var converter = new ObjectToDynamicConverter();

        var listObj = new List<SimpleObject>();
        var obj = new SimpleObject();
        obj.Data = "Test";
        obj.Number = 23;
        listObj.Add(obj);

        var result = converter.Convert(listObj);

        var resultListObj = result as List<ExpandoObject>;

        Assert.IsNotNull(resultListObj);
        Assert.AreEqual(1, resultListObj.Count);
        Assert.IsTrue(resultListObj[0] is ExpandoObject);
        Assert.AreEqual("Test", ((IDictionary<string, object>)resultListObj[0])["Data"]);
        Assert.AreEqual(23, ((IDictionary<string, object>)resultListObj[0])["Number"]);
    }

    [Test]
    public void PrimitivesDictionaryTest()
    {
        var converter = new ObjectToDynamicConverter();

        var dictObject = new Dictionary<string, object>();
        dictObject["Text"] = "Test";
        dictObject["NumberInt"] = 15;
        dictObject["NumberLong"] = (long)150;

        var result = converter.Convert(dictObject);

        Assert.AreEqual("Test", result.Text);
        Assert.AreEqual(15, result.NumberInt);
        Assert.AreEqual(150, result.NumberLong);

        var dictInt = new Dictionary<string, int>();
        dictInt["Number1"] = 15;
        dictInt["Number2"] = 25;

        result = converter.Convert(dictInt);

        Assert.AreEqual(15, result.Number1);
        Assert.AreEqual(25, result.Number2);
    }

    [Test]
    public void ObjectsDictionaryTest()
    {
        var converter = new ObjectToDynamicConverter();

        var dictObject = new Dictionary<string, object>();
        var obj1 = new SimpleObject();
        obj1.Data = "Test";
        obj1.Number = 23;
        dictObject.Add("Nested1", obj1);
        var obj2 = new SimpleObject();
        obj2.Data = "Test1";
        obj2.Number = 230;
        dictObject.Add("Nested2", obj2);
        dictObject.Add("Nested3", null);

        var result = converter.Convert(dictObject);

        Assert.IsNotNull(result.Nested1);
        Assert.AreEqual(typeof(ExpandoObject), result.Nested1.GetType());
        Assert.AreEqual("Test", result.Nested1.Data);
        Assert.AreEqual(23, result.Nested1.Number);
        Assert.IsNotNull(result.Nested2);
        Assert.AreEqual(typeof(ExpandoObject), result.Nested2.GetType());
        Assert.AreEqual("Test1", result.Nested2.Data);
        Assert.AreEqual(230, result.Nested2.Number);
        Assert.IsNull(result.Nested3);
    }

    [Test]
    public void SimpleObjectTest()
    {
        var converter = new ObjectToDynamicConverter();

        var obj = new SimpleObject();
        obj.Data = "Test";
        obj.Number = 23;

        var result = converter.Convert(obj);

        Assert.AreEqual("Test", result.Data);
        Assert.AreEqual(23, result.Number);
    }

    [Test]
    public void ObjectWithNestedObjectTest()
    {
        var converter = new ObjectToDynamicConverter();

        var parent = new ObjectWithNestedObject();
        var obj = new SimpleObject();
        obj.Data = "Test";
        obj.Number = 23;
        parent.NestedObj = obj;

        var result = converter.Convert(parent);

        Assert.AreEqual(typeof(ExpandoObject), result.NestedObj.GetType());
        Assert.AreEqual("Test", result.NestedObj.Data);
        Assert.AreEqual(23, result.NestedObj.Number);
    }

    [Test]
    public void ObjectWithNestedObjectsListTest()
    {
        var converter = new ObjectToDynamicConverter();

        var parent = new ObjectWithNestedObjectsList();
        var obj = new SimpleObject();
        obj.Data = "Test";
        obj.Number = 23;
        parent.NestedObjList = new List<SimpleObject>();
        parent.NestedObjList.Add(obj);

        var result = converter.Convert(parent);

        Assert.AreEqual(typeof(ExpandoObject), result.GetType());
        Assert.AreEqual(typeof(List<ExpandoObject>), result.NestedObjList.GetType());
        var resultList = (List<ExpandoObject>)result.NestedObjList;
        Assert.AreEqual(1, resultList.Count);
        Assert.AreEqual("Test", ((IDictionary<string, object>)resultList[0])["Data"]);
        Assert.AreEqual(23, ((IDictionary<string, object>)resultList[0])["Number"]);
    }

    public class SimpleObject
    {
        public string Data { get; set; }
        public int Number { get; set; }
    }

    public class ObjectWithNestedObject
    {
        public SimpleObject NestedObj { get; set; }
    }

    public class ObjectWithNestedObjectsList
    {
        public List<SimpleObject> NestedObjList { get; set; }
    }
}