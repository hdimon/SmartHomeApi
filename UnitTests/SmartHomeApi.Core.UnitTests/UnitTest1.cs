using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
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
        public async Task TerneoSxTest1()
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

        //[Test]
        public async Task MegaTest1()
        {
            HttpClient client = new HttpClient();

            //var content = new FormUrlEncodedContent(values);

            //var content = new StringContent("{ \"cmd\", 4 }", Encoding.UTF8, "application/json");
            var content = new StringContent("", Encoding.UTF8, "application/text");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/text");

            var response = await client.PostAsync("http://192.168.1.58/get", content);

            var responseString = await response.Content.ReadAsStringAsync();

            Dictionary<string, string> videogames = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

            Assert.Pass();
        }

        //[Test]
        public async Task BreezartReadTest1()
        {
            TcpClient client = new TcpClient("192.168.1.37", 1560);

            Byte[] data = System.Text.Encoding.UTF8.GetBytes($"VSt07_FFFF");
            //Byte[] data = System.Text.Encoding.UTF8.GetBytes($"VPr07_FFFF");

            NetworkStream stream = client.GetStream();

            // Send the message to the connected TcpServer. 
            stream.Write(data, 0, data.Length);

            data = new Byte[256];

            // String to store the response ASCII representation.
            String responseData = String.Empty;

            // Read the first batch of the TcpServer response bytes.
            Int32 bytes = stream.Read(data, 0, data.Length);
            responseData = System.Text.Encoding.UTF8.GetString(data, 0, bytes);

            var result = responseData.Split("_");

            //Mode
            string modeBinarystring = String.Join(String.Empty,
                result[2].Select(
                    c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')
                )
            );

            modeBinarystring = modeBinarystring.PadLeft(16, '0');

            int unitState = Convert.ToInt32(modeBinarystring.Substring(14, 2), 2);
            //Mode

            //Temp
            string tempBinarystring = String.Join(String.Empty,
                result[3].Select(
                    c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')
                )
            );

            tempBinarystring = tempBinarystring.PadLeft(16, '0');

            int setTemp = Convert.ToInt32(tempBinarystring.Substring(0, 8), 2);
            int currentTemp = Convert.ToInt32(tempBinarystring.Substring(8, 8), 2);
            //Temp

            //Speed
            string speedBinarystring = String.Join(String.Empty,
                result[5].Select(
                    c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')
                )
            );

            speedBinarystring = speedBinarystring.PadLeft(16, '0');
            int setSpeed = Convert.ToInt32(speedBinarystring.Substring(8, 4), 2);
            int currentSpeed = Convert.ToInt32(speedBinarystring.Substring(12, 4), 2);
            int factSpeedPercent = Convert.ToInt32(speedBinarystring.Substring(0, 8), 2);
            //Speed

            //Misc
            string miscBinarystring = String.Join(String.Empty,
                result[6].Select(
                    c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')
                )
            );

            miscBinarystring = miscBinarystring.PadLeft(16, '0');
            int filterDust = Convert.ToInt32(miscBinarystring.Substring(0, 8), 2);
            //Misc

            data = System.Text.Encoding.UTF8.GetBytes("VSens_FFFF");

            stream.Write(data, 0, data.Length);

            data = new Byte[256];

            // String to store the response ASCII representation.
            responseData = String.Empty;

            // Read the first batch of the TcpServer response bytes.
            bytes = stream.Read(data, 0, data.Length);
            responseData = System.Text.Encoding.UTF8.GetString(data, 0, bytes);

            result = responseData.Split("_");

            string outTempBinarystring = String.Join(String.Empty,
                result[1].Select(
                    c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')
                )
            );

            outTempBinarystring = outTempBinarystring.PadLeft(16, '0');
            int outTemp = Convert.ToInt32(outTempBinarystring, 2); //Divide on 10

            string powerBinarystring = String.Join(String.Empty,
                result[8].Select(
                    c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')
                )
            );

            powerBinarystring = powerBinarystring.PadLeft(16, '0');
            int power = Convert.ToInt32(powerBinarystring, 2);

            // Close everything.
            stream.Close();
            client.Close();
        }

        [Test]
        public async Task BreezartWriteTest1()
        {
            string turnOnValue = 11.ToString("X").PadLeft(2, '0');
            string turnOfValue = 10.ToString("X").PadLeft(2, '0');

            TcpClient client = new TcpClient("192.168.1.37", 1560);

            Byte[] data1 = System.Text.Encoding.UTF8.GetBytes($"VWPwr_FFFF_");
            Byte[] data = System.Text.Encoding.UTF8.GetBytes($"VWPwr_FFFF_{turnOfValue}");
            //Byte[] data = System.Text.Encoding.UTF8.GetBytes($"VPr07_FFFF");

            NetworkStream stream = client.GetStream();

            // Send the message to the connected TcpServer. 
            stream.Write(data, 0, data.Length);

            data = new Byte[256];

            // String to store the response ASCII representation.
            String responseData = String.Empty;

            // Read the first batch of the TcpServer response bytes.
            Int32 bytes = stream.Read(data, 0, data.Length);
            responseData = System.Text.Encoding.UTF8.GetString(data, 0, bytes);
        }

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
    }
}