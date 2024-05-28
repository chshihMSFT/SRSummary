using Azure.AI.OpenAI;
using Azure;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Diagnostics;

namespace SRSummary
{
    class Program
    {
        //Common Parameters
        static private String OpMode = "";
        static private String OpenAIEndpoint = "";
        static private String OpenAIKey = "";
        static private String OpenAIDeployname = "";
        static private int CompletionDelayMs = 3000;
        static private OpenAIClient openAIClient;
        static private String FilePath = "";        
        static private String FileInput = "";
        static private String FileOutput = "";

        //GPTSummaryInfo parameters
        static private Boolean FileInputwithHeader = true;
        static private String Delimiter = "";
        static private String EndOfLine = "";

        //GPTDOCAbstractInfo parameters
        static private String SkillName = "";
        static private Boolean NeedPreProcessAbstract = false;
        static async Task Main(string[] args)
        {
            //Initialing parameters
            try
            {
                Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Initialing parameters");
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .AddJsonFile("appSettings.json")
                    .Build();

                OpMode = configuration["OpMode"];
                OpenAIEndpoint = configuration["OpenAIEndpoint"];
                OpenAIKey = configuration["OpenAIKey"];
                OpenAIDeployname = configuration["OpenAIDeployname"];
                CompletionDelayMs = Int32.Parse(configuration["CompletionDelayMs"]);
                openAIClient = new(new Uri(OpenAIEndpoint), new AzureKeyCredential(OpenAIKey));

                switch (OpMode.ToLower())
                {
                    case "gptsrsummary":
                        FileInputwithHeader = Boolean.Parse(configuration.GetSection("GPTSummaryInfo")["FileInputwithHeader"]);
                        FilePath = configuration.GetSection("GPTSummaryInfo")["FilePath"];
                        FileInput = configuration.GetSection("GPTSummaryInfo")["FileInput"];
                        FileOutput = configuration.GetSection("GPTSummaryInfo")["FileOutput"];
                        Delimiter = configuration.GetSection("GPTSummaryInfo")["Delimiter"];
                        EndOfLine = configuration.GetSection("GPTSummaryInfo")["EndOfLine"];
                        Console.WriteLine($"========================================");
                        Console.WriteLine($"> Operation Mode: {OpMode}");
                        Console.WriteLine($"> Using Azure OpenAI Endpoint [{OpenAIEndpoint}] with Deployment: [{OpenAIDeployname}]");
                        Console.WriteLine($"> Input File : {FilePath}{FileInput}");
                        Console.WriteLine($"> Output File: {FilePath}{FileOutput}");
                        Console.WriteLine($"> Data Format (Header): IncidentId{Delimiter}Title{Delimiter}IssueDescription{Delimiter}Symptomstxt{EndOfLine}");
                        Console.WriteLine($"========================================");
                        break;
                    case "gptdocabstract":
                        FilePath = configuration.GetSection("GPTDOCAbstractInfo")["FilePath"];
                        FileInput = configuration.GetSection("GPTDOCAbstractInfo")["FileInput"];
                        FileOutput = configuration.GetSection("GPTDOCAbstractInfo")["FileOutput"];
                        SkillName = configuration.GetSection("GPTDOCAbstractInfo")["SkillName"];
                        NeedPreProcessAbstract = Boolean.Parse(configuration.GetSection("GPTDOCAbstractInfo")["NeedPreProcessAbstract"]);
                        Console.WriteLine($"========================================");
                        Console.WriteLine($"> Operation Mode: {OpMode}");
                        Console.WriteLine($"> Using Azure OpenAI Endpoint [{OpenAIEndpoint}] with Deployment: [{OpenAIDeployname}]");
                        Console.WriteLine($"> Input File : {FilePath}{FileInput}");
                        Console.WriteLine($"> Output File: {FilePath}{FileOutput}");
                        Console.WriteLine($"> Data Format: url each line");
                        Console.WriteLine($"> Need Pre Process Abstract: {NeedPreProcessAbstract}");
                        Console.WriteLine($"========================================");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Initialing parameters, error: {ex.Message.ToString()}");
            }

            switch (OpMode.ToLower())
            {
                case "gptsrsummary":
                    await GPT_SRSummary();
                    break;
                case "gptdocabstract":
                    await GPT_DOCAbstract();
                    break;
            }

        }

        public static String GetPromptSRSummary(String Title, String IssueDescription, String Symptomstxt)
        {
            String promptIssue = String.Empty;         //reset prompt

            promptIssue += $"Considering the following described context is mainly a problem that happened in Microsoft Azure Cosmos DB and associated services, so most all terms, technology should be correlated and refer to Azure Services. ";
            promptIssue += $"If there is any Errors or Status Code in the following context, please reference to Cosmos DB HTTP Status code (https://learn.microsoft.com/en-us/rest/api/cosmos-db/http-status-codes-for-cosmosdb) to interpret issue. ";
            if (IssueDescription != String.Empty)
            {
                promptIssue += $"The \"Issue Title\" is '''{Title}''' and \"Issue Description\" provided by customer is following text '''{IssueDescription}''' ";
            }
            if (Symptomstxt != String.Empty && Symptomstxt != "N/A")
            {
                promptIssue += $"And the support engineer determined the \"Symptom\" is following text '''{Symptomstxt}''' ";
            }
            promptIssue += $"DO NOT refer to any information in the following summary include: \"Database name\", \"Collection name\" from both \"Issue Description\" and \"Symptom\". ";
            promptIssue += $"Remove any unknown part from summary. ";
            promptIssue += $"Please provide a straightforward summary to better describe the issue from given \"Issue Description\" and the \"Symptom\" above; ensure removing any personal contact information like name, email address, contact phone number AND account name, database name, container name to protect PRIVACY. ";
            promptIssue += $"The output Summary MUST use a first-person angle to describe the issue. ";

            return promptIssue;
        }
        public static String GetPromptDocAbstract(String SkillName, String Links)
        {
            String prompts = String.Empty;
            prompts += $"Please provide an in-depth abstract from each link.";
            prompts += $"Considering these aspects: " +
                            $"1. Following links are belongs to the Azure Cosmos DB product." +
                            $"2. But you must provide your output in the perspective of topic \"{SkillName}\". " +
                            $"3. HOW the context of given public document supports or be used in this mentioned topic. " +
                            $"4. WHAT the experience and skill you will be able to obtain from the context of given public document. ";
            prompts += $"Link: [{Links}].";
            prompts += $"You answer must follow these rules: " +
                            $"1. Each abstract must be limited between 200 and 300 words, no less than 100. " +
                            $"2. The answer must be written in a professionaletone. " +
                            $"3. The answer must be written in a way that is easy to understand." +
                            $"4. The answer must be written in a way that is concise." +
                            $"5. The answer must be written in a way that is informative." +
                            $"6. Use keywords from the given documents as much as possible. " +
                            $"7. The answer must be written in English. " +
                            $"8. Don't put any comments in your answer but only describe the context straightforwardly. " +
                            //$"9. Use JSON format to output your answer, each tuple needs to include both original link, abstract";
                            $"";

            return prompts;
        }
        public static String GetPromptExtractKeywordsv1(String SkillName, String GPTAbstract)
        {
            String prompts = String.Empty;
            prompts += $"Please extract keywords and short sentences from the given context that can link, associate, represent to the main topic. ";
            prompts += $"Topic: \"{SkillName}]\". ";
            prompts += $"Context: \"{GPTAbstract}\" ";
            prompts += $"You answer must follow these rules: " +
                            $"1. Each tuple is less than 6 words. " +
                            $"2. The answer must be written in English. " +
                            $"3. Use JSON format to output your answer with both \"keywords\" and \"sentences\" parts. " +
                            $"4. The total number of generated Keywords and Sentences both must be between 10 and 15. " +
                            $"5. Each tuple must be unique and MUST order by following priorities: high relationship with the given topic, appear frequency. " +                           
                            $"";

            return prompts;
        }
        public static String GetPromptExtractKeywordsv2(String SkillName, String GPTAbstract)
        {
            String prompts = String.Empty;
            prompts += $"Please extract keywords and short sentences from the given context that can link, associate, represent to the main topic. ";
            prompts += $"The method ";
            prompts += $"Topic: \"{SkillName}]\". ";
            prompts += $"Context: \"{GPTAbstract}\" ";
            prompts += $"You answer must follow these rules: " +
                            $"1. Each tuple is less than 6 words. " +
                            $"2. The answer must be written in English. " +
                            $"3. Use JSON format to output your answer with both \"keywords\" and \"sentences\" parts. " +
                            $"4. The total number of generated Keywords and Sentences both must be between 10 and 15. " +
                            $"5. Each tuple must be unique and MUST order by following priorities: high relationship with the given topic, appear frequency. " +
                            $"6. The method you use MUST follow N-grams rules. " +
                            $"";

            return prompts;
        }


        public static async Task GPT_SRSummary()
        {
            var records = new List<SRInfo>();
            String promptSRSummary = String.Empty;
            String line = String.Empty;
            int i = 1;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Restart();

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

                        promptSRSummary = GetPromptSRSummary(record.Title, record.IssueDescription, record.Symptomstxt);
                        try
                        {
                            record.GPTIssueSummary = await GetGPTChatResponse(promptSRSummary);
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
                            var options = new JsonSerializerOptions
                            {
                                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                            };

                            file.WriteLine(JsonSerializer.Serialize<SRInfo>(record, options));
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
        public static async Task GPT_DOCAbstract()
        {
            var links = new List<SkillLinks>();
            String promptSummaryfromDoclink = String.Empty;
            String line = String.Empty;
            int i = 1;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Restart();

            Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Target Skill Name: {SkillName}");            
            //Step1. pre-processing links.
            Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Step1. pre-processing links.");
            if (NeedPreProcessAbstract)
            {
                try
                {
                    using (var reader = new StreamReader(FilePath + FileInput))
                    {
                        while (!reader.EndOfStream)
                        {
                            var record = new SkillLinks
                            {
                                Link = reader.ReadLine()
                            };
                            links.Add(record);
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
                    Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Processed {links.Count} links in {stopwatch.Elapsed.ToString("hh\\:mm\\:ss\\.fff")}.");
                }
            }
            else
            {
                Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Skip Step1. pre-processing links. ");
            }

            //Step2. pre-processing links.
            stopwatch.Restart();
            Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Step2. Get Summary from each links.");
            if (NeedPreProcessAbstract)
            {
                i = 1;
                //Writing Output File
                using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(FilePath + FileOutput, false)) //true: append, false: overwrite file
                {
                    //clear content of file
                }

                foreach (var item in links)
                {
                    promptSummaryfromDoclink = GetPromptDocAbstract(SkillName, item.Link);
                    item.GPTAbstract = await GetGPTChatResponse(promptSummaryfromDoclink);
                    item.GPTAbstractWordCount = item.GPTAbstract.Split(' ').Length;
                    Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, link {i++}/{links.Count}: {item.Link}");
                    Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, GPTAbstract: {item.GPTAbstract}");

                    //Writing Output File
                    using (System.IO.StreamWriter file =
                        new System.IO.StreamWriter(FilePath + FileOutput, true)) //true: append, false: overwrite file
                    {
                        file.WriteLine(JsonSerializer.Serialize<SkillLinks>(item));
                    }

                    Thread.Sleep(CompletionDelayMs); //preventing 429
                }
            }
            else
            {
                Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Skip Step2. Get Summary from each links. ");
            }

            //Step3. Generate keywords from abstracts
            Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Step3. Generate keywords from abstracts.");
            try
            {
                if (!NeedPreProcessAbstract)
                {
                    using (var reader = new StreamReader(FilePath + FileOutput))
                    {
                        while (!reader.EndOfStream)
                        {
                            var item = JsonSerializer.Deserialize<SkillLinks>(reader.ReadLine().ToString());
                            links.Add(item);
                        }
                    }
                }
                else
                {
                    using (System.IO.StreamWriter file =
                        new System.IO.StreamWriter(FilePath + FileOutput, false)) //true: append, false: overwrite file
                    {
                        //clear content of file
                    }
                }

                i = 1;
                foreach (var item in links)
                {
                    String promptGenKeywords = string.Empty;
                    promptGenKeywords = GetPromptExtractKeywordsv1(SkillName, item.GPTAbstract);
                    String tmpStringsv1 = await GetGPTChatResponse(promptGenKeywords);
                    item.GPTkeywordsv1 = await GetGPTChatResponse(promptGenKeywords);
                    promptGenKeywords = GetPromptExtractKeywordsv2(SkillName, item.GPTAbstract);
                    item.GPTkeywordsv2 = await GetGPTChatResponse(promptGenKeywords);

                    Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, link {i++}/{links.Count}: {item.Link}");
                    Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Generated Keywords_v1: {item.GPTkeywordsv1}");
                    Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Generated Keywords_v2: {item.GPTkeywordsv2}");

                    //Writing Output File
                    using (System.IO.StreamWriter file =
                        new System.IO.StreamWriter(FilePath + FileOutput, true)) //true: append, false: overwrite file
                    {
                        var options = new JsonSerializerOptions
                        {
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        };

                        file.WriteLine(JsonSerializer.Serialize<SkillLinks>(item, options));
                    }

                    Thread.Sleep(CompletionDelayMs); //preventing 429
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Reading & Parsing generated abstract file, error: {ex.Message.ToString()}");
            }
            finally
            {
                stopwatch.Stop();
                Console.WriteLine($"========================================");
                Console.WriteLine($"Processed {links.Count} rows in {stopwatch.Elapsed.ToString("hh\\:mm\\:ss\\.fff")}, " +
                    $"({(links.Count * 1000.0 / stopwatch.ElapsedMilliseconds).ToString("f2")} links/sec) | ({(stopwatch.ElapsedMilliseconds / 1000.0 / links.Count).ToString("f2")} secs/link)");
            }
        }

        public static async Task<String> GetGPTChatResponse(String input)
        {
            var maxTokens = 4096;
            var userMessage = new ChatRequestUserMessage(input);

            ChatCompletionsOptions options = new()
            {
                DeploymentName = OpenAIDeployname,
                Temperature = 0.5f,
                Messages =
                {
                    userMessage
                },
                MaxTokens = maxTokens
            };

            var completionResult = await openAIClient.GetChatCompletionsAsync(options);
            ChatCompletions completions = completionResult.Value;
            var output = completions.Choices[0].Message.Content.ToString();
            output = System.Text.RegularExpressions.Regex.Unescape(output);

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