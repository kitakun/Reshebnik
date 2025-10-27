using Tabligo.Domain.Enums;

namespace Tabligo.Integrations;

public static class IntegrationMetadata
{
    public static Dictionary<IntegrationTypeEnum, (string Name, string Description, bool IsImplemented)> Data = new()
    {
        { IntegrationTypeEnum.GetCourse, ("GetCourse", "LMS and online course platform", true) },
        { IntegrationTypeEnum.GoogleSheets, ("Google Sheets", "Import data from spreadsheets", true) },
        { IntegrationTypeEnum.AmoCRM, ("AmoCRM", "CRM system integration", true) },
        { IntegrationTypeEnum.Bitrix24, ("Bitrix24", "Business management platform", true) },
        { IntegrationTypeEnum.PowerBI, ("Power BI", "Business intelligence and analytics", true) },
        { IntegrationTypeEnum.OneC, ("1C", "Enterprise resource planning system", true) },
        { IntegrationTypeEnum.OnlinePBX, ("Online PBX", "Phone system and call management", true) },
        { IntegrationTypeEnum.YandexDirect, ("Yandex Direct", "Advertising platform", true) },
        { IntegrationTypeEnum.VkAds, ("VK Ads", "Social media advertising", true) },
        { IntegrationTypeEnum.TelegramAds, ("Telegram Ads", "Messaging platform advertising", true) },
        { IntegrationTypeEnum.FacebookAds, ("Facebook Ads", "Social media advertising platform", true) },
        { IntegrationTypeEnum.Elama, ("Elama", "Marketing automation aggregator", true) }
    };
}


