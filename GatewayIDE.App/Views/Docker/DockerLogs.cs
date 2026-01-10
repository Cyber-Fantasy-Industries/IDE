using System.Text;

namespace GatewayIDE.App.ViewModels;

public sealed class DockerLogs : ViewModelBase
{
    private readonly StringBuilder _dockerOut = new();
    private readonly StringBuilder _dockerErr = new();
    private readonly StringBuilder _gatewayLog = new();
    private readonly StringBuilder _containerIO = new();
    private readonly object _gate = new();

    public string GatewayLogBuffer  => _gatewayLog.ToString();
    public string DockerOutBuffer   => _dockerOut.ToString();
    public string DockerErrBuffer   => _dockerErr.ToString();
    public string ContainerIOBuffer => _containerIO.ToString();

    public int GatewayLogCaret  => _gatewayLog.Length;
    public int DockerOutCaret   => _dockerOut.Length;
    public int DockerErrCaret   => _dockerErr.Length;
    public int ContainerIOCaret => _containerIO.Length;

    public void AppendGateway(string s)
    {
        lock (_gate) { _gatewayLog.Append(s); }
        Raise(nameof(GatewayLogBuffer));
        Raise(nameof(GatewayLogCaret));
    }

    public void AppendDockerOut(string s)
    {
        lock (_gate) { _dockerOut.Append(s); }
        Raise(nameof(DockerOutBuffer));
        Raise(nameof(DockerOutCaret));
    }

    public void AppendDockerErr(string s)
    {
        lock (_gate) { _dockerErr.Append(s); }
        Raise(nameof(DockerErrBuffer));
        Raise(nameof(DockerErrCaret));
    }

    public void AppendContainerIO(string s)
    {
        lock (_gate) { _containerIO.Append(s); }
        Raise(nameof(ContainerIOBuffer));
        Raise(nameof(ContainerIOCaret));
    }

    public void ClearAll()
    {
        lock (_gate)
        {
            _dockerOut.Clear();
            _dockerErr.Clear();
            _gatewayLog.Clear();
            _containerIO.Clear();
        }

        Raise(nameof(DockerOutBuffer));   Raise(nameof(DockerOutCaret));
        Raise(nameof(DockerErrBuffer));   Raise(nameof(DockerErrCaret));
        Raise(nameof(GatewayLogBuffer));  Raise(nameof(GatewayLogCaret));
        Raise(nameof(ContainerIOBuffer)); Raise(nameof(ContainerIOCaret));
    }
}
