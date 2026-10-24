using System.Text;
using Pulse.Core;
using Pulse.Core.Api;

namespace Pulse.Cli;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Pipes get UTF-8, which JSON and scripts expect; on Windows .NET would use the console code page.
        // A real console keeps its own encoding, and the console's settings are never changed.
        var stdout = Console.IsOutputRedirected ? Utf8(Console.OpenStandardOutput()) : Console.Out;
        var stderr = Console.IsErrorRedirected ? Utf8(Console.OpenStandardError()) : Console.Error;
        try
        {
            return await CliRunner.RunAsync(args, stdout, stderr, () => ConnectionFile.Read(PulsePaths.Connection));
        }
        finally
        {
            await stdout.FlushAsync();
            await stderr.FlushAsync();
        }
    }

    private static StreamWriter Utf8(Stream stream) => new(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
}
