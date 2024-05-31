using Azure.AI.OpenAI;
using Azure;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Diagnostics;
using System.Xml;
using System.Reflection.PortableExecutable;

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
        static private Boolean NeedReProcessKeywords = false;
        static private Boolean NeedAggregateKeywords = false;
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
                        NeedReProcessKeywords = Boolean.Parse(configuration.GetSection("GPTDOCAbstractInfo")["NeedReProcessKeywords"]);
                        NeedAggregateKeywords = Boolean.Parse(configuration.GetSection("GPTDOCAbstractInfo")["NeedAggregateKeywords"]);
                        Console.WriteLine($"========================================");
                        Console.WriteLine($"> Operation Mode: {OpMode}");
                        Console.WriteLine($"> Using Azure OpenAI Endpoint [{OpenAIEndpoint}] with Deployment: [{OpenAIDeployname}]");
                        Console.WriteLine($"> Input File : {FilePath}{FileInput}");
                        Console.WriteLine($"> Output File: {FilePath}{FileOutput}");
                        Console.WriteLine($"> Data Format: url each line");
                        Console.WriteLine($"> Need Pre Process Abstract: {NeedPreProcessAbstract}");
                        Console.WriteLine($"> Need Re Process Keywords: {NeedReProcessKeywords}");
                        Console.WriteLine($"> Need Aggregate Keywords: {NeedAggregateKeywords}");
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

            Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Finished.");
        }

        public static String GetPromptSRSummary(String Title, String IssueDescription, String Symptomstxt)
        {
            String promptIssue = String.Empty;

            String PromptVersion = "PromptV2-2024-May"; //Prompt Version            
            switch(PromptVersion)
            {
                case "PromptV2-2024-May":
                    #region PromptV1-2024-May
                    promptIssue += $"" +
                        $"As a technical support analyst for \"Azure Cosmos DB\" you need to generate text summarization for the incident statement summary of incident text 'Title' and detailed Incident text 'Issue description' and an 'LLM_Identified_SG' based on the summary. "+
                        $"Based on the Title and Issue description, classify the incident into 1 or more of the following required skill categories to solve the cases, also using the related keywords given in brackets for each of the skill categories. " +
                        $"If the Title and Issue description contains strict keywords given in curly brackets of any of the skill categories, then that should be the skill category identified despite relevance found for any other skill categories. " +
                        $"The related keywords is supposed to assist in finding relevance, but strict keywords show strong association to the skill category and strict keywords should take precedence over the related keywords: ";
                    promptIssue += $"" +
                        $"skill category: \"Java SDK, NodeJS or JavaScript SDK\", keywords: [Java SDK,NodeJS,JavaScript SDK,Azure Cosmos DB,SDK v4,Java applications,CRUD operations,CosmosClient,configure database,execute operations,querying with SQL,handling pagination,managing throughput,error handling,connection management,performance tuning,code snippets,step-by-step approach,advanced querying techniques,database interactions,robust applications,scalable applications,full capabilities,reliability,performance,configure connections,SQL querying,setting up SDK,Java SDK v4,best practices,initialize CosmosClient,creating items,reading items,updating items,deleting items,optimizing performance,database and container,NodeJS SDK,multi-model capabilities,document data model,key-value data model,graph data model,column-family data model,global distribution,low latency,high availability,consistency models,API support,SQL API,MongoDB API,Cassandra API,Gremlin API,Table API,partitioning,indexing,query performance,storage efficiency,high-performance applications,Table APIs,distributed applications]";
                    promptIssue += $"" +
                        $"You output must follow these rules: " +
                        $"*. Retaining the original text as much as possible, create a focused summary of problem statement for Title and Issue description Separately, but add more targeted keywords and the related keywords identified in the last step that would point to the top-most related skill category identified, removing words that target the other Skill groups. " +
                        $"*. Do not add any other information here. Provide the text alone without the square brackets. " +
                        $"*. The output must be translated into English. " +
                        //$"*. If the identified top-most related skill category is not Performance, then for 'LLM_Identified_SG' assign the identified top-most related skill category alone as the value, else assign 'Performance' string as its value. " +
                        $"*. Do not include anything about 'LLM_Identified_SG' in the Title or Issue description summary and vice versa. " +
                        $"*. Do not add any specifics to PII data specifics like server name, IP address or or any customer related information in Title and Issue description. " +
                        $"*. If the problem description is too vague to provide any of the details return only \"Description too vague.\" " +
                        //$"*. Always provide the output with all the below columns delimited by ';' in the exact following format:\\nTitleSummary: [TitleSummary];\\nIssueDescriptionSummary: [IssueDescriptionSummary]; \\nLLM_Identified_SG: [LLM_Identified_SG];\\\"\\n\\nThe incident text is the following:\\nTitle Text:[@{{items('Apply_to_each_2')?['Traning Title']}}], IssueDescription Text:[@{{items('Apply_to_each_2')?['Traning Verbatim']}}] \\nPlease proceed with the analysis and provide the required output.  ";
                        $"*. Always provide the output with all the below columns in the exact following JSON format " +
                        $"{{TitleSummary: [TitleSummary], IssueDescriptionSummary: [IssueDescriptionSummary]; LLM_Identified_SG: [LLM_Identified_SG]}}. "+
                        $"*. Remove any markdown syntax like '''json''', only use plaintext and unescaped string in your output. ";
                        //$"The incident text is the following:  Title Text:[@{{items('Apply_to_each_2')?['Traning Title']}}]" +
                        //$", IssueDescription Text:[@{{items('Apply_to_each_2')?['Traning Verbatim']}}] \\n.  ";
                    promptIssue += $"" +
                        $"Please proceed with the analysis and provide the required output. " +
                        $"Here is the input for you: " +
                        $"> Title: {Title}. " +
                        $"> IssueDescription: {IssueDescription}. ";
                    #endregion
                    break;
                case "PromptV1-2024-Feb":
                default:
                    #region PromptV1-2024-Feb
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
                    promptIssue += $"The output format needs to use plain-text only, do not use any markdown syntax and \"Summary\" to format your answer. ";
                    #endregion
                    break;
            }

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
                            $"2. The answer must be translated into English. " +
                            $"3. The answer must be written in a professionaletone. " +
                            $"4. The answer must be written in a way that is easy to understand." +
                            $"5. The answer must be written in a way that is concise." +
                            $"6. The answer must be written in a way that is informative." +
                            $"7. Use keywords from the given documents as much as possible. " +
                            $"8. The answer must be written in English. " +
                            $"9. Don't put any comments in your answer but only describe the context straightforwardly. " +
                            //$"9. Use JSON format to output your answer, each tuple needs to include both original link, abstract";
                            $"";

            return prompts;
        }
        public static String GetPromptExtractKeywordsv1(String SkillName, String GPTAbstract)
        {
            String prompts = String.Empty;
            prompts += $"As a SEO engineer, you are required to extract single keywords, or short phrases from the given abstract that can be linked, associated, represent to the main topic. ";
            prompts += $"Topic: \"{SkillName}]\". ";
            prompts += $"Context: \"{GPTAbstract}\" ";
            prompts += $"You answer must follow these rules: " +
                            $"1. Each string is less than 8 words. " +
                            $"2. The answer must be written in English. " +
                            $"3. Use JSON format to output your answer with a \"keywords\" string array. " +
                            $"4. The total number of generated keywords (or short phrases) both must be between 15 and 25. " +
                            $"5. Each tuple must be unique and MUST order by following priorities: high relationship with the given topic, appear frequency. " +
                            $"6. Remove any markdown syntax like '''json''', only use plaintext and unescaped string in your output. " +
                            $"";

            return prompts;
        }
        public static String GetPromptExtractKeywordsv2(String SkillName, String GPTAbstract)
        {
            String prompts = String.Empty;
            prompts += $"As a SEO engineer, you are required to extract single keywords, or short phrases from the given abstract that can be linked, associated, represent to the main topic. ";
            prompts += $"Topic: \"{SkillName}]\". ";
            prompts += $"Context: \"{GPTAbstract}\" ";
            prompts += $"You answer must follow these rules: " +
                            $"1. Each string is less than 8 words. " +
                            $"2. The answer must be written in English. " +
                            $"3. Use JSON format to output your answer with a \"keywords\" string array. " +
                            $"4. The total number of generated keywords (or short phrases) both must be between 15 and 25. " +
                            $"5. Each tuple must be unique and MUST order by following priorities: high relationship with the given topic, appear frequency. " +
                            $"6. Remove any markdown syntax like '''json''', only use plaintext and unescaped string in your output. " +
                            $"7. The method you use MUST follow N-grams rules. " +
                            $"";

            return prompts;
        }

        public static async Task GPT_SRSummary()
        {
            var records = new List<SRInfo>();
            String promptSRSummary = String.Empty;
            String line = String.Empty;
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

                    int i = 1;
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
                            Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, row {i}, \n" +
                                $"> IncidentId: {record.IncidentId} \n" +
                                $"> Title: {record.Title}\n" +
                                $"> IssueDescription: {record.IssueDescription}\n" +
                                $"> Symptomstxt: {record.Symptomstxt}\n" +
                                $"==> \n" +
                                $"GPTIssueSummary: {record.GPTIssueSummary} \n"
                                );
                        }

                        //Writing Output File
                        using (System.IO.StreamWriter file =
                            new System.IO.StreamWriter(FilePath + FileOutput, (i == 1 ? false : true))) //true: append, false: overwrite file
                        {
                            var options = new JsonSerializerOptions
                            {
                                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                            };

                            file.WriteLine(JsonSerializer.Serialize<SRInfo>(record, options));
                        }
                        i++;
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


            Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Target Skill Name: {SkillName}");
            //Step1. pre-processing links.
            stopwatch.Restart();
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
                    Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Step1, error: {ex.Message.ToString()}");
                }
                finally
                {
                    stopwatch.Stop();
                    //Console.WriteLine($"========================================");
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
                try
                {
                    i = 1;
                    foreach (var item in links)
                    {
                        promptSummaryfromDoclink = GetPromptDocAbstract(SkillName, item.Link);
                        item.SkillName = Program.SkillName;
                        item.GPTAbstract = await GetGPTChatResponse(promptSummaryfromDoclink);
                        item.GPTAbstractWordCount = item.GPTAbstract.Split(' ').Length;
                        Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, link {i}/{links.Count}: {item.Link}");
                        Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, GPTAbstract: {item.GPTAbstract}");

                        //Writing Output File
                        using (System.IO.StreamWriter file =
                            new System.IO.StreamWriter(FilePath + FileOutput, (i == 1 ? false : true))) //true: append, false: overwrite file
                        {
                            file.WriteLine(JsonSerializer.Serialize<SkillLinks>(item));
                        }
                        i++;
                        Thread.Sleep(CompletionDelayMs); //preventing 429
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Step2, error: {ex.Message.ToString()}");
                }
                finally
                {
                    stopwatch.Stop();
                    //Console.WriteLine($"========================================");
                    Console.WriteLine($"Processed {links.Count} links in {stopwatch.Elapsed.ToString("hh\\:mm\\:ss\\.fff")}, " +
                        $"({(links.Count * 1000.0 / stopwatch.ElapsedMilliseconds).ToString("f2")} links/sec) | ({(stopwatch.ElapsedMilliseconds / 1000.0 / links.Count).ToString("f2")} secs/link)");
                }
            }
            else
            {
                Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Skip Step2. Get Summary from each links. ");
            }

            //Step3. Generate keywords from abstracts
            Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Step3. Generate keywords from each abstracts.");
            if (NeedReProcessKeywords)
            {
                if (links.Count == 0)
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

                try
                {
                    i = 1;
                    foreach (var item in links)
                    {
                        String promptGenKeywords = string.Empty;
                        promptGenKeywords = GetPromptExtractKeywordsv1(SkillName, item.GPTAbstract);
                        String tmpStringsv1 = await GetGPTChatResponse(promptGenKeywords);
                        item.GPTkeywordsv1 = await GetGPTChatResponse(promptGenKeywords);
                        promptGenKeywords = GetPromptExtractKeywordsv2(SkillName, item.GPTAbstract);
                        item.GPTkeywordsv2 = await GetGPTChatResponse(promptGenKeywords);

                        Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, link {i}/{links.Count}: {item.Link}");
                        Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Generated Keywords_v1: {item.GPTkeywordsv1}");
                        Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Generated Keywords_v2: {item.GPTkeywordsv2}");

                        //Writing Output File
                        using (System.IO.StreamWriter file =
                            new System.IO.StreamWriter(FilePath + FileOutput, (i == 1 ? false : true))) //true: append, false: overwrite file
                        {
                            var options = new JsonSerializerOptions
                            {
                                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                            };

                            file.WriteLine(JsonSerializer.Serialize<SkillLinks>(item, options));
                        }
                        i++;
                        Thread.Sleep(CompletionDelayMs); //preventing 429
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Step3, error: {ex.Message.ToString()}");
                }
                finally
                {
                    stopwatch.Stop();
                    Console.WriteLine($"========================================");
                    Console.WriteLine($"Processed {links.Count} rows in {stopwatch.Elapsed.ToString("hh\\:mm\\:ss\\.fff")}, " +
                        $"({(links.Count * 1000.0 / stopwatch.ElapsedMilliseconds).ToString("f2")} links/sec) | ({(stopwatch.ElapsedMilliseconds / 1000.0 / links.Count).ToString("f2")} secs/link)");
                }
            }
            else
            {
                Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Skip Step3. Generate keywords from each abstracts. ");
            }

            //Step 4. Aggregate keywords from all generated keywords
            Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Step4. Aggregate keywords from all generated keywords.");
            if (NeedAggregateKeywords)
            {
                if (links.Count == 0)
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

                Dictionary<String, int> keywords = new Dictionary<String, int>();
                try
                {
                    i = 1;
                    foreach (var item in links)
                    {
                        KeywordsObject GPTkeywordsv1 = JsonSerializer.Deserialize<KeywordsObject>(item.GPTkeywordsv1.ToString());
                        KeywordsObject GPTkeywordsv2 = JsonSerializer.Deserialize<KeywordsObject>(item.GPTkeywordsv2.ToString());

                        if (GPTkeywordsv1.keywords != null)
                        {
                            foreach (var keyword in GPTkeywordsv1.keywords)
                            {
                                if (keywords.ContainsKey(keyword))
                                {
                                    keywords[keyword] += 1;
                                }
                                else
                                {
                                    keywords.Add(keyword, 1);
                                }
                            }
                        }
                        if (GPTkeywordsv2.keywords != null)
                        {
                            foreach (var keyword in GPTkeywordsv2.keywords)
                            {
                                if (keywords.ContainsKey(keyword))
                                {
                                    keywords[keyword] += 1;
                                }
                                else
                                {
                                    keywords.Add(keyword, 1);
                                }
                            }
                        }

                        Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, link {i}/{links.Count}: {item.Link}");
                        i++;
                        Thread.Sleep(CompletionDelayMs); //preventing 429            
                    }
                    
                    /*
                    foreach (var item in keywords.OrderByDescending(key => key.Value))
                    {
                        Console.WriteLine("{0}, Value: {1}", item.Key, item.Value);
                    }
                    */
                    String AggregateKeywords  = String.Join(",", keywords.OrderByDescending(key => key.Value).Select(x => x.Key));
                    String AggregateKeywordsandCounts = String.Join(",", keywords.OrderByDescending(key => key.Value).Select(x => x.Key + ":" + x.Value));

                    Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Aggregate keywords: \n{AggregateKeywords}");
                    Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Aggregate keywords and counts: \n{AggregateKeywordsandCounts}");

                    //Writing Output File
                    FileOutput = FileOutput.Replace(".json", "_AggregateKeywords.json");
                    using (System.IO.StreamWriter file =
                        new System.IO.StreamWriter(FilePath + FileOutput, false)) //true: append, false: overwrite file
                    {
                        var options = new JsonSerializerOptions
                        {
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        };

                        file.WriteLine($"AggregateKeywords: \n{AggregateKeywords}");
                        file.WriteLine($"AggregateKeywordsandCounts: \n{AggregateKeywordsandCounts}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Step4, error: {ex.Message.ToString()}");
                }
                finally
                {
                    stopwatch.Stop();
                    Console.WriteLine($"========================================");
                    Console.WriteLine($"Processed {links.Count} rows in {stopwatch.Elapsed.ToString("hh\\:mm\\:ss\\.fff")}, " +
                        $"({(links.Count * 1000.0 / stopwatch.ElapsedMilliseconds).ToString("f2")} links/sec) | ({(stopwatch.ElapsedMilliseconds / 1000.0 / links.Count).ToString("f2")} secs/link)");
                }

            }
            else
            {
                Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff")}, Skip Step4. Aggregate keywords from all generated keywords. ");
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