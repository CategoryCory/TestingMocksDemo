using Temperature.Contracts;
using TemperatureMonitor.Contracts;
using TemperatureMonitor.Events;

namespace TemperatureMonitor.Alerts;

public sealed class AlertBus : IAlertBus
{
    public event EventHandler<AlertRaisedEventArgs>? AlertRaised;
    public void Raise(TemperatureAnalysisResult result)
        => AlertRaised?.Invoke(this, new AlertRaisedEventArgs(result));

}
