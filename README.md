# Mossharbor.AzureWorkArounds.QnaMaker
.net wrapper around Microsofts QnaMaker REST api for managing knowledge bases.

Install the nuget package:  [Install-Package Mossharbor.AzureWorkArounds.QnaMaker -Version 1.0.1](https://www.nuget.org/packages/Mossharbor.AzureWorkArounds.QnaMaker/1.0.1#)

*Example:*
```cs
using Mossharbor.AzureWorkArounds.QnaMaker;

// Ask a question from an existing KB
QnAKnowledgebase kb =new QnAKnowledgebase(<qna service name>, <knowledge base name>, <ocp-apim-subscription-key>); // TODO enter your credentials in here!!
var answers = kb.GenerateAnswer("hi");

// Modifying existing Knowledgebase
QnaKnowledgebaseBuilder builder = new QnaKnowledgebaseBuilder();

builder
    .Modify(maker)
    .AddAnswerToQuestions("Hello", new string[] {"Hello", "There"})
    .Update();

kb.Publish();

// Creating a new Knowledgebase
builder  
    .Create(<qna service name>, <knowledge base name>, <ocp-apim-subscription-key>)
    .AddAnswerToQuestions("Hello", new string[] {"Hello", "There"})
    .UpdateKnowledgebase();
    
// or 
QnAKnowledgebase kb =new QnAKnowledgebase(<qna service name>, <knowledge base name>, <ocp-apim-subscription-key>); // TODO enter your credentials in here!!
kb.CreateIfDoesntExist()


// and more quick options for modifying the knowledgebase
List<string> answers = maker.GetAnswerStrings();
List<string> questions = maker.GetQuestionsFor("Hello");
maker.DeleteQuestion("Hello", "There");

```
