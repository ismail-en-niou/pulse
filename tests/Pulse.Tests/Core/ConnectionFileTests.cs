using Pulse.Core.Api;
using Pulse.Core.Platform;

namespace Pulse.Tests.Core;

public sealed class ConnectionFileTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "pulse-tests-" + Guid.NewGuid().ToString("N"));
    private readonly RecordingWriter _writer = new();

    private string ConnectionPath => Path.Combine(_directory, "connection.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void A_missing_file_reads_as_null_and_gives_a_new_token()
    {
        Assert.Null(ConnectionFile.Read(ConnectionPath));
        Assert.Equal(43, ConnectionFile.LoadOrCreateToken(ConnectionPath).Length);
    }

    [Fact]
    public void The_token_survives_a_restart()
    {
        var token = ConnectionInfo.NewToken();
        ConnectionFile.Write(_writer, ConnectionPath, new ConnectionInfo(0, token, 0));

        Assert.Equal(token, ConnectionFile.LoadOrCreateToken(ConnectionPath));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("not a token at all, it has spaces and is long enough!!")]
    public void A_token_that_is_not_ours_is_replaced(string token)
    {
        ConnectionFile.Write(_writer, ConnectionPath, new ConnectionInfo(0, token, 0));

        var replaced = ConnectionFile.LoadOrCreateToken(ConnectionPath);

        Assert.NotEqual(token, replaced);
        Assert.Equal(43, replaced.Length);
    }

    [Fact]
    public void Write_goes_through_the_user_only_writer_and_reads_back()
    {
        var info = new ConnectionInfo(52731, ConnectionInfo.NewToken(), 42);

        ConnectionFile.Write(_writer, ConnectionPath, info);

        Assert.Equal([ConnectionPath], _writer.Paths);
        Assert.Equal(info, ConnectionFile.Read(ConnectionPath));
    }

    [Fact]
    public void The_file_can_be_read_while_another_reader_holds_it_open()
    {
        ConnectionFile.Write(_writer, ConnectionPath, new ConnectionInfo(1, ConnectionInfo.NewToken(), 1));
        using var held = new FileStream(ConnectionPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

        Assert.NotNull(ConnectionFile.Read(ConnectionPath));
    }

    /// <summary>Plain file writes; the real permissions are covered by the platform tests.</summary>
    private sealed class RecordingWriter : IUserOnlyFileWriter
    {
        public List<string> Paths { get; } = [];

        public void Write(string path, string contents)
        {
            Paths.Add(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, contents);
        }
    }
}
