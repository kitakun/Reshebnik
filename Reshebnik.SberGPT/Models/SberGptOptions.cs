namespace Reshebnik.SberGPT.Models;

public class SberGptOptions
{
    public string AuthData { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://gigachat.devices.sberbank.ru/api/v1";
    public string OAuthUrl { get; set; } = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";
    public string GrpcEndpoint { get; set; } = "gigachat.devices.sberbank.ru";
    public int GrpcPort { get; set; } = 443;
    public bool UseGrpc { get; set; } = true;
    public string Model { get; set; } = "GigaChat:latest";
    public int MaxTokens { get; set; } = 1000;
    public double Temperature { get; set; } = 0.7;
    public double TopP { get; set; } = 0.9;
}
