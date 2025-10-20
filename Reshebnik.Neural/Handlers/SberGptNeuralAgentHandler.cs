using Microsoft.Extensions.Logging;
using Reshebnik.Domain.Models.Neural;
using Reshebnik.Neural.Interfaces;
using Reshebnik.SberGPT.Services;
using System.Text;
using System.Text.Json;

namespace Reshebnik.Neural.Handlers;

public class SberGptNeuralAgentHandler(
    ISberGptService sberGptService,
    ILogger<SberGptNeuralAgentHandler> logger)
    : ITabligoNeuralAgent
{
    public async Task<NeuralResponse> ProcessFileAsync(string fileContent, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Processing file {FileName} with SberGPT neural agent", fileName);

            // System prompt with comprehensive system information
            var systemPrompt = GetSystemPrompt();
            
            // User prompt with file content
            var userPrompt = $@"
Проанализируйте следующее содержимое файла и предложите сущности, которые должны быть созданы в системе Reshebnik.

Имя файла: {fileName}
Содержимое файла:
{fileContent}

На основе содержимого файла, пожалуйста, предложите, какие сущности (Компании, Сотрудники, Департаменты, Метрики, Индикаторы) должны быть созданы. 
Верните ваш ответ в формате JSON со следующей структурой:
{{
  ""suggestedEntities"": [
    {{
      ""entityType"": ""Company|Employee|Department|Metric|Indicator"",
      ""name"": ""Название сущности"",
      ""description"": ""Описание сущности"",
      ""properties"": {{
        ""CompanyId"": ""temp-company-12345678-1234-1234-1234-123456789abc"",
        ""DepartmentId"": ""temp-dept-87654321-4321-4321-4321-cba987654321"",
        ""EmployeeId"": ""temp-emp-11111111-2222-3333-4444-555555555555"",
        ""MetricId"": ""temp-metric-aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"",
        ""другие_свойства"": ""значения""
      }},
      ""confidence"": 0.95,
      ""reasoning"": ""Объяснение, почему эта сущность должна быть создана""
    }}
  ],
  ""analysisSummary"": ""Сводка анализа""
}}

ВАЖНО: 
1. Обязательно включайте соответствующие GUID в поле properties для создания связей между сущностями!
2. НЕ ИСПОЛЬЗУЙТЕ КОММЕНТАРИИ В JSON - только чистый валидный JSON без // или /* */ комментариев!";

            // Combine system and user prompts into a single request
            var combinedPrompt = $"{systemPrompt}\n\n{userPrompt}";
            
            // Use streaming for large responses
            var responseBuilder = new StringBuilder();
            await foreach (var chunk in sberGptService.HandleStreamAsync(combinedPrompt, cancellationToken))
            {
                responseBuilder.Append(chunk);
            }
            var response = responseBuilder.ToString();
            
            if (string.IsNullOrEmpty(response))
            {
                return new NeuralResponse
                {
                    IsSuccessful = false,
                    ErrorMessage = "Пустой ответ от SberGPT"
                };
            }

            // Parse the JSON response from SberGPT
            try
            {
                // Extract JSON from markdown code blocks if present
                var cleanJson = ExtractJsonFromResponse(response);
                
                // Remove any potential comments from JSON
                cleanJson = RemoveJsonComments(cleanJson);
                
                var neuralResponse = JsonSerializer.Deserialize<NeuralResponse>(cleanJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                });

                if (neuralResponse == null)
                {
                    logger.LogWarning("SberGPT response could not be deserialized into NeuralResponse");
                    return new NeuralResponse
                    {
                        IsSuccessful = false,
                        ErrorMessage = "Не удалось распарсить ответ SberGPT в формат NeuralResponse"
                    };
                }

                // Ensure the response is properly structured
                neuralResponse.IsSuccessful = true;
                
                // Validate that we have the expected structure
                if (neuralResponse.SuggestedEntities == null)
                {
                    neuralResponse.SuggestedEntities = new List<SuggestedEntity>();
                }
                
                if (string.IsNullOrEmpty(neuralResponse.AnalysisSummary))
                {
                    neuralResponse.AnalysisSummary = "Анализ успешно завершен";
                }

                logger.LogInformation("Successfully processed file {FileName} with {EntityCount} suggested entities", 
                    fileName, neuralResponse.SuggestedEntities.Count);
                
                return neuralResponse;
            }
            catch (JsonException jsonEx)
            {
                logger.LogError(jsonEx, "JSON parsing error for file {FileName}. Raw response: {Response}", fileName, response);
                
                // Try to create a fallback response with the raw text
                return CreateFallbackResponse(response, fileName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing file {FileName} with SberGPT", fileName);
            return new NeuralResponse
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private string ExtractJsonFromResponse(string response)
    {
        logger.LogDebug("Extracting JSON from SberGPT response. Original length: {Length}", response.Length);
        
        // Remove markdown code blocks
        if (response.Contains("```json"))
        {
            var startIndex = response.IndexOf("```json") + 7;
            var endIndex = response.LastIndexOf("```");
            if (endIndex > startIndex)
            {
                response = response.Substring(startIndex, endIndex - startIndex).Trim();
                logger.LogDebug("Extracted JSON from markdown code block. Length: {Length}", response.Length);
            }
        }
        else if (response.Contains("```"))
        {
            var startIndex = response.IndexOf("```") + 3;
            var endIndex = response.LastIndexOf("```");
            if (endIndex > startIndex)
            {
                response = response.Substring(startIndex, endIndex - startIndex).Trim();
                logger.LogDebug("Extracted content from generic code block. Length: {Length}", response.Length);
            }
        }

        // Remove any leading/trailing whitespace and newlines
        response = response.Trim();

        // If the response doesn't start with {, try to find the JSON object
        if (!response.StartsWith("{"))
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                response = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                logger.LogDebug("Extracted JSON object from text. Length: {Length}", response.Length);
            }
        }

        // Log the cleaned JSON for debugging
        logger.LogDebug("Cleaned JSON: {Json}", response.Length > 500 ? response[..500] + "..." : response);
        
        return response;
    }

    private string RemoveJsonComments(string json)
    {
        if (string.IsNullOrEmpty(json))
            return json;

        // Remove single-line comments (//)
        var lines = json.Split('\n');
        var cleanLines = new List<string>();
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Skip empty lines and comment-only lines
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//"))
                continue;
                
            // Remove inline comments
            var commentIndex = trimmedLine.IndexOf("//");
            if (commentIndex >= 0)
            {
                var beforeComment = trimmedLine.Substring(0, commentIndex).Trim();
                if (!string.IsNullOrEmpty(beforeComment))
                {
                    cleanLines.Add(beforeComment);
                }
            }
            else
            {
                cleanLines.Add(line);
            }
        }
        
        return string.Join("\n", cleanLines);
    }

    private NeuralResponse CreateFallbackResponse(string rawResponse, string fileName)
    {
        logger.LogWarning("Creating fallback NeuralResponse for file {FileName} due to JSON parsing failure", fileName);
        
        return new NeuralResponse
        {
            IsSuccessful = false,
            ErrorMessage = "Не удалось распарсить ответ SberGPT в структурированный формат",
            AnalysisSummary = $"Анализ не удался для файла {fileName}. Ответ не удалось распарсить в ожидаемый JSON формат.",
            SuggestedEntities = new List<SuggestedEntity>()
        };
    }

    private static string GetSystemPrompt()
    {
        return @"Вы - ИИ-ассистент для системы Reshebnik (управление эффективностью предприятия).

## Сущности:
- **Company**: Name, Industry, EmployeesCount, Type (LegalEntity/Individual/SelfEmployed), Email, Phone, **ExternalId** (уникальный идентификатор из документа)
- **Employee**: FIO (обязательно), JobTitle, Email, Phone, DefaultRole (Supervisor/Employee), CompanyId, DepartmentId, **ExternalId** (уникальный идентификатор из документа)
- **Department**: Name (обязательно), Comment, CompanyId, **ExternalId** (уникальный идентификатор из документа), **ParentDepartmentId** (если есть родительский департамент)
- **Metric**: Name (обязательно), Description, Unit (Count/Percent), Type (PlanFact/FactOnly/Cumulative), PeriodType (Day/Week/Month/Quartal/Year), CompanyId, DepartmentId?, EmployeeId?, **ExternalId** (уникальный идентификатор из документа)
- **Indicator**: Name (обязательно), Category, Description, UnitType, FillmentPeriod, ValueType, CompanyId, DepartmentId?, EmployeeId?, **ExternalId** (уникальный идентификатор из документа)

## Связи (обязательные GUID):
- CompanyId: ""temp-company-[8]-[4]-[4]-[4]-[12]""
- DepartmentId: ""temp-dept-[8]-[4]-[4]-[4]-[12]""
- EmployeeId: ""temp-emp-[8]-[4]-[4]-[4]-[12]""
- MetricId: ""temp-metric-[8]-[4]-[4]-[4]-[12]""

## КРИТИЧЕСКИ ВАЖНО:
1. ВСЕГДА включайте ВСЕ найденные свойства в поле ""properties"" - НЕ ИГНОРИРУЙТЕ дополнительные данные!
2. Если в тексте есть дополнительные поля (например, адрес, дата создания, статус, и т.д.) - ОБЯЗАТЕЛЬНО включите их в properties
3. **ОБЯЗАТЕЛЬНО** извлекайте или генерируйте ExternalId для каждой сущности:
   - СНАЧАЛА ищите в документе существующие ID (employee_id, metric_code, department_id, company_id) - используйте их как ExternalId
   - Если существующий ID не найден - создайте временный GUID в формате ""temp-[тип]-[8 символов]-[4 символа]-[4 символа]-[4 символа]-[12 символов]""
   - ExternalId должен быть уникальным в рамках компании
   - НЕ используйте одинаковые ExternalId для разных сущностей

4. **СВЯЗЫВАЙТЕ ДЕПАРТАМЕНТЫ** если в документе есть иерархия:
   - Если департамент имеет родительский департамент - укажите ParentDepartmentId
   - ParentDepartmentId должен ссылаться на ExternalId родительского департамента
   - Анализируйте структуру организации и создавайте правильную иерархию
5. Используйте точные значения из текста, не придумывайте
6. JSON без комментариев, только валидный JSON
7. Все на русском языке

Формат ответа:
{
  ""suggestedEntities"": [
    {
      ""entityType"": ""Company|Employee|Department|Metric|Indicator"",
      ""name"": ""Название"",
      ""description"": ""Описание"",
      ""properties"": {
        ""ExternalId"": ""найденный_или_сгенерированный_id"",
        ""CompanyId"": ""temp-company-12345678-1234-1234-1234-123456789abc"",
        ""другие_найденные_поля"": ""значения_из_текста""
      },
      ""confidence"": 0.95,
      ""reasoning"": ""Обоснование""
    }
  ],
  ""analysisSummary"": ""Сводка""
}";
    }

}
