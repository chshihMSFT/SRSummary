# Operate Mode: GPTSRSummary
Combining with OpenAI GPT, this program is designed to understand input SR Title and Description to summarize the SR is most relevant to which Skill Group.  
With the given restrict keywords for each Skill Groups (embedded in prompts), the program will cross check input context to identify the most relevant Skill Group and re-generate a copy of title, description based on the context.

## Step 1. Consider following sample KQL to extract required information from SRs

```
let Delimiter = '||:||';
let EndOfLine = '|:EOL:|';
AllCloudsSupportIncidentWithReferenceModelJitVNext
| where SapSupportPathL3 in ('SQL API SDK - NET') 
    and RootCauseL2 in ('SQL API SDK')
    and RootCauseL3 in ('.NET & .NET Core')
| where SupportLanguage in ('en-US')
//<Start: remove unnecessary PII reserved words >
| extend
    Title = replace_regex(Title, @'\{(?:NAME|ADDRESS|PHONE|ALPHANUMERIC|EMAIL|IP|UNC|CREDITCARD|NAME\+NAME|PHONE\+PHONE|ALPHANUMERIC\+IP|ALPHANUMERIC\+EMAIL|ALPHANUMERIC\+PHONE)PII\}', '')
    , IssueDescription = replace_regex(IssueDescription, @'\{(?:NAME|ADDRESS|PHONE|ALPHANUMERIC|EMAIL|IP|UNC|CREDITCARD|NAME\+NAME|PHONE\+PHONE|ALPHANUMERIC\+IP|ALPHANUMERIC\+EMAIL|ALPHANUMERIC\+PHONE)PII\}', '')
    , Symptomstxt = replace_regex(Symptomstxt, @'\{(?:NAME|ADDRESS|PHONE|ALPHANUMERIC|EMAIL|IP|UNC|CREDITCARD|NAME\+NAME|PHONE\+PHONE|ALPHANUMERIC\+IP|ALPHANUMERIC\+EMAIL|ALPHANUMERIC\+PHONE)PII\}', '')
| extend IssueDescription = replace_string(replace_string(replace_string(IssueDescription, @'{ALPHANUMERIC+PHONE+PHONE+PHONE+PHONE+PHONE+PHONEPII}', ''), @'{ALPHANUMERIC+PHONE+IPPII}', ''), @'{ALPHANUMERIC+PHONE+PHONE+PHONE+PHONEPII}', '')
//<End: remove unnecessary PII reserved words >
//<Start: filter only those ill-format data and non-necessary records>
| where IssueDescription !contains @'This support ticket was created specifically to track an Azure chat'
    and IssueDescription !contains @'[Azure Government]'
| where IssueDescription contains @'<Start:Agent_Additional_Properties_Do_Not_Edit>' 
    and IssueDescription contains @'<End:Agent_Additional_Properties_Do_Not_Edit>' 
| where IssueDescription contains @"Question:" and IssueDescription contains @"Answer:"
| parse IssueDescription with IssueDescriptionBody '<Start:Agent_Additional_Properties_Do_Not_Edit>' Agent_Additional_Properties_Do_Not_Edit '<End:Agent_Additional_Properties_Do_Not_Edit>' AdditionalInfoIfAny
| extend IssueDescriptionBody = split(IssueDescriptionBody, 'Answer:')
| mv-expand IssueDescriptionBody
| where IssueDescriptionBody !contains 'Question:'
    and IssueDescriptionBody !contains 'Environment Information'
| summarize IssueDescriptionBody = strcat_array(make_list(IssueDescriptionBody),'') by IncidentId, Title, Symptomstxt
| where array_length(split(IssueDescriptionBody, ' ')) > 10
//<End: filter only those ill-format data >
| extend
    Title = trim(@"\s+", Title)
    , IssueDescriptionBody = trim(@"\s+", IssueDescriptionBody)
    , Symptomstxt = trim(@"\s+", Symptomstxt)
| limit 10
| order by IncidentId asc
| extend outputCSV = strcat(IncidentId, Delimiter, Title, Delimiter, IssueDescriptionBody, Delimiter, Symptomstxt, EndOfLine)
| project outputCSV
```

## Step 2. Save the exported value [outputCSV] into local file (ex: input-sample.csv).
Ensure the format of each record is concat properly with specified "Delimiter" and "EndOfLine".
If there exist header of columns in the file, please specify **\{FileInputwithHeader : True\}** in the following step.
```
IncidentId||:||Title||:||IssueDescription||:||Symptomstxt|:EOL:|
1234567890123456||:||This is a sample title||:||encounter 408 in Cosmos DB||:||customer encountered 408|:EOL:|
```

## Step 3. Configure program parameters accordingly
Please ensure the value of **"Delimiter"** and **"EndOfLine"** parameters are aligned with Step 1 you used.
```
{
  "FilePath": "C:\\temp\\",
  "FileInputwithHeader": true,
  "FileInput": "input-sample.csv",
  "FileOutput": "output-sample.json",
  "Delimiter": "||:||",
  "EndOfLine": "|:EOL:|",
  "OpenAIEndpoint": "https://<EndPoint>.openai.azure.com",
  "OpenAIKey": "AccessKey",
  "OpenAIDeployname": "DeploymentName",
  "CompletionDelayMs": 3000 //preventing 429 - https://learn.microsoft.com/en-us/azure/ai-services/openai/quotas-limits
}
```

## Step 4. Customize prompt question to GPT based on your team requirements
### Sample 
>   
Considering the following described context is mainly a problem that happened in Microsoft Azure Cosmos DB and associated services, so most all terms, technology should be correlated and refer to Azure Services.  
If there is any Errors or Status Code in the following context, please reference to Cosmos DB HTTP Status code (https://learn.microsoft.com/en-us/rest/api/cosmos-db/http-status-codes-for-cosmosdb) to interpret issue.  
The \"Issue Title\" is '''___replaceWithTitle___''' and \"Issue Description\" provided by customer is following text '''___replaceWithIssueDescription___'''  
And the support engineer determined the \"Symptom\" is following text '''___replaceWithSymptomstxt___'''  
DO NOT refer to any information in the following summary include: \"Database name\", \"Collection name\" from both \"Issue Description\" and \"Symptom\".  
Remove any unknown part from summary.  
Please provide a straightforward summary to better describe the issue from given \"Issue Description\" and the \"Symptom\" above; ensure removing any personal contact information like name, email address, contact phone number AND account name, database name, container name to protect PRIVACY.  
The output Summary MUST use a first-person angle to describe the issue.  

## Reference & Demo
[input-sample.csv](SRSummary/resources/input-sample.csv)  
[output-sample.json](SRSummary/resources/output-sample.json)  
![Sample](SRSummary/resources/sample.png)  

<hr>

# Operate Mode: GPTDOCAbstract

(tbd)
