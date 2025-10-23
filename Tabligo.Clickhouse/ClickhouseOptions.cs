namespace Tabligo.Clickhouse;

public class ClickhouseOptions
{
    public string Prefix { get; set; } = null!;
    public string Host { get; set; } = null!;
    public string Port { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string DbName { get; set; } = null!;
}