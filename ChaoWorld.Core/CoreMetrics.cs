using App.Metrics;
using App.Metrics.Gauge;
using App.Metrics.Meter;
using App.Metrics.Timer;

namespace ChaoWorld.Core
{
    public static class CoreMetrics
    {
        public static GaugeOptions GardenCount => new GaugeOptions { Name = "Gardens", MeasurementUnit = Unit.Items };
        public static GaugeOptions ChaoCount => new GaugeOptions { Name = "Chao", MeasurementUnit = Unit.Items };
        public static GaugeOptions ProcessPhysicalMemory => new GaugeOptions { Name = "Process Physical Memory", MeasurementUnit = Unit.Bytes, Context = "Process" };
        public static GaugeOptions ProcessVirtualMemory => new GaugeOptions { Name = "Process Virtual Memory", MeasurementUnit = Unit.Bytes, Context = "Process" };
        public static GaugeOptions ProcessPrivateMemory => new GaugeOptions { Name = "Process Private Memory", MeasurementUnit = Unit.Bytes, Context = "Process" };
        public static GaugeOptions ProcessThreads => new GaugeOptions { Name = "Process Thread Count", MeasurementUnit = Unit.Threads, Context = "Process" };
        public static GaugeOptions ProcessHandles => new GaugeOptions { Name = "Process Handle Count", MeasurementUnit = Unit.Items, Context = "Process" };
        public static GaugeOptions CpuUsage => new GaugeOptions { Name = "CPU Usage", MeasurementUnit = Unit.Percent, Context = "Process" };

        public static MeterOptions DatabaseRequests => new MeterOptions() { Name = "Database Requests", MeasurementUnit = Unit.Requests, Context = "Database", RateUnit = TimeUnit.Seconds };
        public static TimerOptions DatabaseQuery => new TimerOptions() { Name = "Database Query", MeasurementUnit = Unit.Requests, DurationUnit = TimeUnit.Seconds, RateUnit = TimeUnit.Seconds, Context = "Database" };
        public static GaugeOptions DatabaseConnections => new GaugeOptions() { Name = "Database Connections", MeasurementUnit = Unit.Connections, Context = "Database" };
    }
}