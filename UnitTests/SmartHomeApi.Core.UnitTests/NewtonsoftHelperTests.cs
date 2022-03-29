using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using Common.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace SmartHomeApi.Core.UnitTests;

internal class NewtonsoftHelperTests
{
    [Test]
    public void PrimitivesTest()
    {
        //Only int, long, double and boolean primitives are supported as valid json.
        var json = "17";
        var token = JToken.Parse(json);
        var result = NewtonsoftHelper.ParseJTokenAsExpando(token);
        Assert.AreEqual(typeof(long), result.GetType());
        Assert.AreEqual(17, result);

        json = "20.5";
        token = JToken.Parse(json);
        result = NewtonsoftHelper.ParseJTokenAsExpando(token);
        Assert.AreEqual(typeof(double), result.GetType());
        Assert.AreEqual(20.5, result);

        json = "true";
        token = JToken.Parse(json);
        result = NewtonsoftHelper.ParseJTokenAsExpando(token);
        Assert.IsTrue((bool)result);

        json = "false";
        token = JToken.Parse(json);
        result = NewtonsoftHelper.ParseJTokenAsExpando(token);
        Assert.IsFalse((bool)result);

        bool failed = false;

        try
        {
            var date = new DateTime(2022, 2, 2, 15, 11, 12);
            json = date.ToString(CultureInfo.InvariantCulture);
            token = JToken.Parse(json);
            NewtonsoftHelper.ParseJTokenAsExpando(token);
        }
        catch (JsonReaderException)
        {
            failed = true;
        }

        Assert.IsTrue(failed);
    }

    [Test]
    public void ObjectsTest()
    {
        var json = JsonConvert.SerializeObject(new SimpleObject { Number = 12, Data = "Test string" });
        var token = JToken.Parse(json);
        var result = NewtonsoftHelper.ParseJTokenAsExpando(token);
        Assert.AreEqual(typeof(ExpandoObject), result.GetType());
        var expando = result as IDictionary<string, object>;
        Assert.IsNotNull(expando);
        Assert.AreEqual(12, expando["Number"]);
        Assert.AreEqual("Test string", expando["Data"]);
    }

    [Test]
    public void PrimitivesListsTest()
    {
        var json = JsonConvert.SerializeObject(new List<int> { 12, 45 });
        var token = JToken.Parse(json);
        var result = NewtonsoftHelper.ParseJTokenAsExpando(token);
        Assert.AreEqual(typeof(List<object>), result.GetType());
        var list = (List<object>)result;
        Assert.AreEqual(2, list.Count);
        Assert.AreEqual(12, list[0]);
        Assert.AreEqual(45, list[1]);
    }

    [Test]
    public void PrimitivesListOfListsTest()
    {
        var json = JsonConvert.SerializeObject(new List<List<int>> { new() { 12 } });
        var token = JToken.Parse(json);
        var result = NewtonsoftHelper.ParseJTokenAsExpando(token);
        Assert.AreEqual(typeof(List<object>), result.GetType());
        var list = (List<object>)result;
        Assert.AreEqual(1, list.Count);
        Assert.AreEqual(typeof(List<object>), list[0].GetType());
        var nestedList = (List<object>)list[0];
        Assert.AreEqual(1, nestedList.Count);
        Assert.AreEqual(12, nestedList[0]);
    }

    [Test]
    public void ObjectsListsTest()
    {
        var json = JsonConvert.SerializeObject(new List<SimpleObject>
        {
            new() { Number = 12, Data = "Test string" }
        });
        var token = JToken.Parse(json);
        var result = NewtonsoftHelper.ParseJTokenAsExpando(token);
        Assert.AreEqual(typeof(List<object>), result.GetType());
        var list = (List<object>)result;
        Assert.AreEqual(1, list.Count);
        Assert.AreEqual(typeof(ExpandoObject), list[0].GetType());
        var expando = list[0] as IDictionary<string, object>;
        Assert.IsNotNull(expando);
        Assert.AreEqual(12, expando["Number"]);
        Assert.AreEqual("Test string", expando["Data"]);
    }

    public class SimpleObject
    {
        public string Data { get; set; }
        public int Number { get; set; }
    }
}