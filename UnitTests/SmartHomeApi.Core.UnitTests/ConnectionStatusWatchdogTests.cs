using System.Threading.Tasks;
using NUnit.Framework;
using SmartHomeApi.Core.Interfaces;
using SmartHomeApi.ItemUtils;

namespace SmartHomeApi.Core.UnitTests
{
    class ConnectionStatusWatchdogTests
    {
        [Test]
        public async Task InitializeTest1()
        {
            string status;
            var watchdog = new ConnectionStatusWatchdog(1, 2, s =>
            {
                status = s;

                Assert.AreEqual(ConnectionStatus.Unknown, status);
            });
            await watchdog.Initialize();

            watchdog.Dispose();
        }

        [Test]
        public async Task UnstableConnectionTest1()
        {
            int counter = 0;
            string status;
            var watchdog = new ConnectionStatusWatchdog(1, 5, s => {
                status = s;

                if (counter == 0)
                {
                    Assert.AreEqual(ConnectionStatus.Unknown, status);
                    counter++;
                }
                else if (counter == 1)
                {
                    Assert.AreEqual(ConnectionStatus.Unstable, status);
                    counter++;
                }
            });
            await watchdog.Initialize();

            await Task.Delay(3);
            watchdog.Dispose();
        }

        [Test]
        public async Task LostConnectionTest1()
        {
            string status;
            int counter = 0;

            var watchdog = new ConnectionStatusWatchdog(1, 5, s =>
            {
                status = s;

                if (counter == 0)
                {
                    Assert.AreEqual(ConnectionStatus.Unknown, status);
                    counter++;
                }
                else if (counter == 1)
                {
                    Assert.AreEqual(ConnectionStatus.Unstable, status);
                    counter++;
                }
                else if (counter == 2)
                {
                    Assert.AreEqual(ConnectionStatus.Lost, status);
                    counter++;
                }
            });
            await watchdog.Initialize();

            await Task.Delay(10);
            watchdog.Dispose();
        }

        [Test]
        public async Task ResetTest1()
        {
            string status;
            int counter = 0;
            var watchdog = new ConnectionStatusWatchdog(50, 100, s =>
            {
                status = s;

                if (counter == 0)
                {
                    Assert.AreEqual(ConnectionStatus.Unknown, status);
                    counter++;
                }
                else
                    Assert.AreEqual(ConnectionStatus.Stable, status);
            });

            await watchdog.Initialize();

            watchdog.Reset();
            await Task.Delay(20);
            Assert.AreEqual(ConnectionStatus.Stable, watchdog.GetStatus());

            watchdog.Reset();
            await Task.Delay(20);
            Assert.AreEqual(ConnectionStatus.Stable, watchdog.GetStatus());

            watchdog.Reset();
            await Task.Delay(20);
            Assert.AreEqual(ConnectionStatus.Stable, watchdog.GetStatus());

            watchdog.Reset();
            await Task.Delay(20);
            Assert.AreEqual(ConnectionStatus.Stable, watchdog.GetStatus());

            watchdog.Dispose();
        }

        [Test]
        public async Task FullFlowTest1()
        {
            string status;
            int counter = 0;
            var watchdog = new ConnectionStatusWatchdog(50, 100, s =>
            {
                status = s;

                if (counter == 0)
                {
                    Assert.AreEqual(ConnectionStatus.Unknown, status);
                    counter++;
                }
                else if (counter == 1)
                {
                    Assert.AreEqual(ConnectionStatus.Stable, status);
                    counter++;
                }
                else if (counter == 2)
                {
                    Assert.AreEqual(ConnectionStatus.Unstable, status);
                    counter++;
                }
                else if (counter == 3)
                {
                    Assert.AreEqual(ConnectionStatus.Lost, status);
                    counter++;
                }
                else if (counter == 4)
                {
                    Assert.AreEqual(ConnectionStatus.Stable, status);
                    counter++;
                }
                else if (counter == 5)
                {
                    Assert.AreEqual(ConnectionStatus.Unstable, status);
                    counter++;
                }
                else if (counter == 6)
                {
                    Assert.AreEqual(ConnectionStatus.Lost, status);
                    counter++;
                }
            });

            await watchdog.Initialize();

            await Task.Delay(20);
            watchdog.Reset(); //50 ms left till Unstable
            await Task.Delay(20);
            Assert.AreEqual(ConnectionStatus.Stable, watchdog.GetStatus());
            //30 ms left till Unstable

            await Task.Delay(40); //40 ms left till Lost
            Assert.AreEqual(ConnectionStatus.Unstable, watchdog.GetStatus());

            await Task.Delay(20); //20 ms left till Lost
            Assert.AreEqual(ConnectionStatus.Unstable, watchdog.GetStatus());

            await Task.Delay(30); //90 ms left till Lost
            Assert.AreEqual(ConnectionStatus.Lost, watchdog.GetStatus());

            await Task.Delay(100); //90 ms left till Lost
            Assert.AreEqual(ConnectionStatus.Lost, watchdog.GetStatus());

            await Task.Delay(100); //90 ms left till Lost
            Assert.AreEqual(ConnectionStatus.Lost, watchdog.GetStatus());

            watchdog.Reset(); //50 ms left till Unstable
            await Task.Delay(20); //30 ms left till Unstable
            Assert.AreEqual(ConnectionStatus.Stable, watchdog.GetStatus());

            await Task.Delay(150);
            Assert.AreEqual(ConnectionStatus.Lost, watchdog.GetStatus());

            watchdog.Dispose();
        }
    }
}