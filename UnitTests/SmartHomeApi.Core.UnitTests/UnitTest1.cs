using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using SmartHomeApi.DeviceUtils;

namespace SmartHomeApi.Core.UnitTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        //[Test]
        public async Task Test1()
        {
            HttpClient client = new HttpClient();
            var values = new Dictionary<string, string>
            {
                { "cmd", "4" }
            };

            //var content = new FormUrlEncodedContent(values);

            //var content = new StringContent("{ \"cmd\", 4 }", Encoding.UTF8, "application/json");
            var content = new StringContent("{\"cmd\":4}", Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await client.PostAsync("http://192.168.1.48/api.cgi", content);

            var responseString = await response.Content.ReadAsStringAsync();

            Dictionary<string, string> videogames = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

            Assert.Pass();
        }

        [Test]
        public void Test2()
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
    }
}