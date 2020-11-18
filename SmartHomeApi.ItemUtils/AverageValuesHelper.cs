using System.Collections.Concurrent;
using System.Linq;

namespace SmartHomeApi.ItemUtils
{
    public class AverageValuesHelper
    {
        private readonly int _historyCount;

        private readonly ConcurrentQueue<double> _values = new ConcurrentQueue<double>();

        public AverageValuesHelper(int historyCount)
        {
            _historyCount = historyCount;
        }

        public double GetAverageValue(double currentValue)
        {
            _values.Enqueue(currentValue);

            if (_values.Count > _historyCount)
                _values.TryDequeue(out _);

            var averageValue = _values.Average();

            return averageValue;
        }

        public double GetAverageValue()
        {
            var averageValue = _values.Average();

            return averageValue;
        }
    }
}