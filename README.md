# Mossharbor.AzureWorkArounds.QnaMaker
.net wrapper around Microsofts QnaMaker REST api for managing knowledge bases.

Install the nuget package:  [Install-Package Mossharbor.AzureWorkArounds.QnaMaker -Version 1.0.1](https://www.nuget.org/packages/Mossharbor.AzureWorkArounds.QnaMaker/1.0.1#)

*Example:*
```cs
using Mossharbor.AzureWorkArounds.QnaMaker;

// Ask a question
QnAKnowledgebase kb =new QnAKnowledgebase(<qna service name>, <knowledge base name>, <ocp-apim-subscription-key>); // TODO enter your credentials in here!!
var answers = kb.GenerateAnswer("hi");

// Building/Modifying existing QnA information
QnaKnowledgebaseBuilder builder = new QnaKnowledgebaseBuilder();

// add new answer and questions to your qna knowledgebase
builder
    .Modify(maker)
    .AddAnswerToQuestions("Hello", new string[] {"Hello", "There"})
    .Update();

kb.Publish();

List<string> answers = maker.GetAnswerStrings();
List<string> questions = maker.GetQuestionsFor("Hello");
maker.DeleteQuestion("Hello", "There");

```
