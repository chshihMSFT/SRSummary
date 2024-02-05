using Azure.AI.OpenAI;
using Azure;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Diagnostics;

namespace SRSummary
{
    class Program
    {
        static private String OpenAIEndpoint = "";
        static private String OpenAIKey = "";
        static private String OpenAIDeployname = "";
        static private int CompletionDelayMs = 3000;
        static private OpenAIClient openAIClient;
        static private String FilePath = "";        
        static private Boolean FileInputwithHeader = true;
        static private String FileInput = "";
        static private String FileOutput = "";
        static private String Delimiter = "";
        static private String EndOfLine = "";
        static async Task Main(string[] args)
        {
            //Initialing parameters
            try
            {
                Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Initialing parameters");
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .AddJsonFile("appSettings.json")
                    .Build();

                OpenAIEndpoint = configuration["OpenAIEndpoint"];
                OpenAIKey = configuration["OpenAIKey"];
                OpenAIDeployname = configuration["OpenAIDeployname"];
                CompletionDelayMs = Int32.Parse(configuration["CompletionDelayMs"]);
                openAIClient = new(new Uri(OpenAIEndpoint), new AzureKeyCredential(OpenAIKey));                
                
                FilePath = configuration["FilePath"];
                FileInputwithHeader = Boolean.Parse(configuration["FileInputwithHeader"]);
                FileInput = configuration["FileInput"];
                FileOutput = configuration["FileOutput"];
                Delimiter = configuration["Delimiter"];
                EndOfLine = configuration["EndOfLine"];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Initialing parameters, error: {ex.Message.ToString()}");
            }
            finally
            {
                Console.WriteLine($"========================================");
                Console.WriteLine($"> Using Azure OpenAI Endpoint [{OpenAIEndpoint}] with Deployment: [{OpenAIDeployname}]");
                Console.WriteLine($"> Input File : {FilePath}{FileInput}");
                Console.WriteLine($"> Output File: {FilePath}{FileOutput}");
                Console.WriteLine($"> Data Format (Header): IncidentId{Delimiter}Title{Delimiter}IssueDescription{Delimiter}Symptomstxt{EndOfLine}");
                Console.WriteLine($"========================================");
            }

            //Reading Input File
            var records = new List<SRInfo>();
            String promptIssue = String.Empty;
            String line = String.Empty;
            int i = 1;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                using (var reader = new StreamReader(FilePath + FileInput))
                {
                    if (FileInputwithHeader)
                    {
                        reader.ReadLine();
                    }

                    while (!reader.EndOfStream)
                    {
                        line = reader.ReadLine();
                        while (!line.Contains(EndOfLine))
                        {
                            line += reader.ReadLine();
                        }
                        var values = line.Split(Delimiter);

                        var record = new SRInfo
                        {
                            IncidentId = values[0].Replace(EndOfLine, String.Empty),
                            Title = values[1].Replace(EndOfLine, String.Empty),
                            IssueDescription = values[2].Replace(EndOfLine, String.Empty),
                            Symptomstxt = values[3].Replace(EndOfLine, String.Empty)
                        };

                        promptIssue = String.Empty;         //reset prompt
                        #region promptDesign
                        promptIssue += $"Considering the following described context is mainly a problem that happened in Microsoft Azure Cosmos DB and associated services, so most all terms, technology should be correlated and refer to Azure Services. ";
                        promptIssue += $"If there is any Errors or Status Code in the following context, please reference to Cosmos DB HTTP Status code (https://learn.microsoft.com/en-us/rest/api/cosmos-db/http-status-codes-for-cosmosdb) to interpret issue. ";
                        if (record.IssueDescription != String.Empty)
                        {
                            promptIssue += $"The \"Issue Title\" is '''{record.Title}''' and \"Issue Description\" provided by customer is following text '''{record.IssueDescription}''' ";
                        }
                        if (record.Symptomstxt != String.Empty && record.Symptomstxt != "N/A")
                        {
                            promptIssue += $"And the support engineer determined the \"Symptom\" is following text '''{record.Symptomstxt}''' ";
                        }
                        promptIssue += $"DO NOT refer to any information in the following summary include: \"Database name\", \"Collection name\" from both \"Issue Description\" and \"Symptom\". ";
                        promptIssue += $"Remove any unknown part from summary. ";
                        promptIssue += $"Please provide a straightforward summary to better describe the issue from given \"Issue Description\" and the \"Symptom\" above; ensure removing any personal contact information like name, email address, contact phone number AND account name, database name, container name to protect PRIVACY. ";
                        promptIssue += $"The output Summary MUST use a first-person angle to describe the issue. ";
                        #endregion

                        try
                        {
                            record.GPTIssueSummary = await generateSummary(promptIssue);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")} generateSummary Exception: {ex.ToString()}");
                        }
                        finally
                        {
                            records.Add(record);
                            Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, row {i++}, IncidentId: {record.IncidentId}, GPTIssueSummary: {record.GPTIssueSummary}");
                        }

                        //Writing Output File
                        using (System.IO.StreamWriter file =
                            new System.IO.StreamWriter(FilePath + FileOutput, true)) //true: append, false: overwrite file
                        {

                            file.WriteLine(JsonSerializer.Serialize<SRInfo>(record));
                        }

                        Thread.Sleep(CompletionDelayMs); //preventing 429
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Reading & Parsing File, error: {ex.Message.ToString()}");
            }
            finally
            {
                stopwatch.Stop();
                Console.WriteLine($"========================================");
                Console.WriteLine($"Processed {records.Count} rows in {stopwatch.Elapsed.ToString("hh\\:mm\\:ss\\.fff")}, " +
                    $"({(records.Count * 1000.0 / stopwatch.ElapsedMilliseconds).ToString("f2")} rows/sec) | ({(stopwatch.ElapsedMilliseconds / 1000.0 / records.Count).ToString("f2")} secs/row)");
            }
        }

        public static async Task<String> generateSummary(string input)
        {
            var maxTokens = 2000;
            var userMessage = new ChatRequestUserMessage(input);

            ChatCompletionsOptions options = new()
            {
                DeploymentName = OpenAIDeployname,
                Messages =
                {
                    userMessage
                },
                MaxTokens = maxTokens
            };

            var completionResult = await openAIClient.GetChatCompletionsAsync(options);
            ChatCompletions completions = completionResult.Value;
            var output = completions.Choices[0].Message.Content.ToString();

            bool Debug = false;
            if (Debug)
            {
                Console.WriteLine($"DEBUG===================================");
                Console.WriteLine($"[OpenAI Input]: {input}");                
                Console.WriteLine($"[OpenAI Output]: {completions.Choices[0].Message.Content}");
                Console.WriteLine($"DEBUG===================================");
            }

            return output;
        }

    }

}