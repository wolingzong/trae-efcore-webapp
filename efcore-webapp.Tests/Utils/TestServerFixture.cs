using System.Diagnostics;

namespace EfCoreWebApp.Tests.Utils;

public class TestServerFixture : IDisposable
{
    private Process? _process;
    public string BaseUrl { get; } = "http://localhost:5001";

    public TestServerFixture()
    {
        // Check if port is available or assume it's freed by previous cleanup.
        // We will start the process using "dotnet EfCoreWebApp.dll" assuming it's in the same directory.
        
        var assemblyLocation = typeof(TestServerFixture).Assembly.Location;
        var directory = Path.GetDirectoryName(assemblyLocation);
        var appDll = Path.Combine(directory!, "EfCoreWebApp.dll");

        if (!File.Exists(appDll))
        {
            throw new FileNotFoundException($"Could not find application DLL at {appDll}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{appDll}\" --urls \"{BaseUrl}\"",
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        // Ensure environment variables are set if needed
        startInfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development";

        _process = new Process { StartInfo = startInfo };
        
        _process.OutputDataReceived += (sender, args) => Console.WriteLine($"[AppOut]: {args.Data}");
        _process.ErrorDataReceived += (sender, args) => Console.WriteLine($"[AppErr]: {args.Data}");

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        // Wait for server to be ready
        // Simple wait, or check connectivity loop
        WaitForServer(BaseUrl).Wait();
    }

    private async Task WaitForServer(string url)
    {
        using var client = new HttpClient();
        var retries = 20;
        while (retries > 0)
        {
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Found) // 302 is ok?
                {
                    // Server is up
                    return;
                }
            }
            catch
            {
                // Ignore connection refused
            }
            
            await Task.Delay(500);
            retries--;
        }
        
        // If we get here, check if process exited
        if (_process != null && _process.HasExited)
        {
             throw new Exception("Server process exited prematurely.");
        }
        
        throw new Exception("Server timed out starting up.");
    }

    public void Dispose()
    {
        if (_process != null && !_process.HasExited)
        {
            try
            {
                _process.Kill(entireProcessTree: true); // .NET 6+ API
            }
            catch
            {
                // Ignore
            }
            _process.Dispose();
        }
    }
}
