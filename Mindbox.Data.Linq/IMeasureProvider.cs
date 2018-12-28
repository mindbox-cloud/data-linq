namespace System.Data.Linq
{
    public interface IMeasureProvider
    {
        IDisposable Measure(string metricName);
    }
}