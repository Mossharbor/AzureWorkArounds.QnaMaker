# Mossharbor.AzureWorkArounds.QnaMaker
.net wrapper around Microsofts QnaMaker REST api for managing knowledge bases.

Install the nuget package:  [Install-Package Mossharbor.AzureWorkArounds.QnaMaker -Version 1.0.1](https://www.nuget.org/packages/Mossharbor.AzureWorkArounds.QnaMaker/1.0.1#)

*Example:*
```cs
using Mossharbor.AzureWorkArounds.QnaMaker;

// TODO enter your credentials in here!!
QnAMaker maker =new QnAMaker("", "", "", "");
QnaUpdateBuilder builder = new QnaUpdateBuilder();

// add new answer and questions to your qna knowledgebase
builder
    .Begin(maker)
    .AddQuestionAndAnswer("Hello", new string[] {"Hello", "There"})
    .Update();

List<string> answers = maker.GetAnswerStrings();
List<string> questions = maker.GetQuestionsFor("Hello");
maker.DeleteQuestion("Hello", "There");

```