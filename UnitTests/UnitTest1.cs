using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    using Mossharbor.AzureWorkArounds.QnaMaker;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;

    [TestClass]
    public class QnaMakerUnitTests
    {
        List<QnAMaker> makers = new List<QnAMaker>();

        private QnAMaker GetQnaMaker(string kbToUseForTesting = "qnamakertestkb", bool createIfNotExist = true)
        {
            string ocpApimSubscriptionKey = ConfigurationManager.AppSettings["ocpApimSubscriptionKey"];

            var maker = new QnAMaker(ConfigurationManager.AppSettings["qnaMakerName"], kbToUseForTesting, ocpApimSubscriptionKey);
            if (createIfNotExist)
                maker.CreateKnowledgeBaseIfDoesntExist();
            makers.Add(maker);
            return maker;
        }

        [TestMethod]
        public void CreateEmptyKB()
        {
            QnAMaker qna = null;
            try
            {
                qna = GetQnaMaker(nameof(CreateEmptyKB));

                string kb = qna.GetKnowledgebaseJson();
                Assert.IsTrue(kb.Contains("qnaDocuments"));
            }
            finally
            {
                qna.DeleteKnowledgeBase();
            }
        }

        [TestMethod]
        public void GetAnswer()
        {
            var qna = GetQnaMaker(nameof(GetAnswer));
            try
            {
                QnaUpdateBuilder builder = new QnaUpdateBuilder();
                bool success = builder
                                .Begin(qna)
                                .AddAnswerToQuestion("Sunny", "What is the weather like?")
                                .UpdateKnowledgebase();

                // qna.Train();
                // qna.Publish();

                var answers = qna.GenerateAnswer("What is the weather like?");

                bool exists = answers.FirstOrDefault(p => p.answer == "Sunny") != null;
                Assert.IsTrue(exists);
            }
            finally
            {
                qna.DeleteKnowledgeBase();
            }
        }

        [TestMethod]
        public void GetAnswerAfterPublishFromProd()
        {
            var qna = GetQnaMaker(nameof(GetAnswer));
            try
            {
                QnaUpdateBuilder builder = new QnaUpdateBuilder();
                bool success = builder
                                .Begin(qna)
                                .AddAnswerToQuestion("Sunny", "What is the weather like?")
                                .UpdateKnowledgebase();

                Assert.IsTrue(success);

                // qna.Train();
                qna.Publish();

                success = builder
                                .Begin(qna)
                                .AddAnswerToQuestion("Cloudy", "What is the weather like?")
                                .UpdateKnowledgebase();

                var answers = qna.GenerateAnswer("What is the weather like?", EnvironmentType.Prod);

                bool sunny = answers.FirstOrDefault(p => p.answer == "Sunny") != null;
                bool cloudy = answers.FirstOrDefault(p => p.answer == "Cloudy") != null;
                Assert.IsTrue(sunny);
                Assert.IsFalse(cloudy);

                answers = qna.GenerateAnswer("What is the weather like?", EnvironmentType.Test);

                cloudy = answers.FirstOrDefault(p => p.answer == "Cloudy") != null;
                Assert.IsTrue(cloudy);
            }
            finally
            {
                qna.DeleteKnowledgeBase();
            }
        }

        [TestMethod]
        public void AddQuestionAndAnswer()
        {
            QnAMaker maker = GetQnaMaker(nameof(AddQuestionAndAnswer));
            try
            {
                QnaUpdateBuilder builder = new QnaUpdateBuilder();
                bool success = builder
                                .Begin(maker)
                                .AddAnswerToQuestion("Hello", "Hello")
                                .UpdateKnowledgebase();

                Assert.IsTrue(success);

                IEnumerable<string> answers = maker.GetAnswerStrings();
                Assert.IsTrue(answers.Contains("Hello"));
                IEnumerable<string> questions = maker.GetQuestionsFor("Hello");
                Assert.IsTrue(questions.Contains("Hello"));
            }
            finally
            {
                maker.DeleteKnowledgeBase();
            }
        }

        [TestMethod]
        public void AddMultipleQuestionAndAnswer()
        {
            QnAMaker maker = GetQnaMaker(nameof(AddMultipleQuestionAndAnswer));
            Assert.IsTrue(maker.GetDetails() != null);
            try
            {
                QnaUpdateBuilder builder = new QnaUpdateBuilder();
                bool success = builder
                                .Begin(maker)
                                .AddAnswerToQuestions("Hello", new string[] { "Hello", "There" })
                                .UpdateKnowledgebase();

                Assert.IsTrue(success);
                System.Threading.Thread.Sleep(1000);

                IEnumerable<string> answers = maker.GetAnswerStrings();
                Assert.IsTrue(answers.Contains("Hello"));

                IEnumerable<string> questions = maker.GetQuestionsFor("Hello");
                Assert.IsTrue(questions.Contains("Hello"));
                Assert.IsTrue(questions.Contains("There"));
            }
            finally
            {
                maker.DeleteKnowledgeBase();
            }
        }

        [TestMethod]
        public void UpdateExistingAnswerWithAdditionalQuestions()
        {
            QnAMaker maker = GetQnaMaker(nameof(UpdateExistingAnswerWithAdditionalQuestions));
            try
            {
                QnaUpdateBuilder builder = new QnaUpdateBuilder();
                bool success = builder
                                .Begin(maker)
                                .AddAnswerToQuestions("Hello", new string[] { "Hello"})
                                .UpdateKnowledgebase();

                Assert.IsTrue(success);

                IEnumerable<string> answers = maker.GetAnswerStrings();
                Assert.IsTrue(answers.Contains("Hello"));

                IEnumerable<string> questions = maker.GetQuestionsFor("Hello");
                Assert.IsTrue(questions.Contains("Hello"));
                Assert.IsFalse(questions.Contains("There"));

                success = builder
                                .Begin(maker)
                                .AddAnswerToQuestions("Hello", new string[] { "There" })
                                .UpdateKnowledgebase();
                Assert.IsTrue(success);
                questions = maker.GetQuestionsFor("Hello");
                Assert.IsTrue(questions.Count() == 2);
                Assert.IsTrue(questions.Contains("There"));
                Assert.IsTrue(questions.Contains("Hello"));
            }
            finally
            {
                maker.DeleteKnowledgeBase();
            }
        }

        [TestMethod]
        public void DeleteAnswer()
        {
            QnAMaker maker = GetQnaMaker(nameof(DeleteAnswer));
            try
            {
                QnaUpdateBuilder builder = new QnaUpdateBuilder();
                bool success = builder
                                .Begin(maker)
                                .AddAnswerToQuestion("Hello", "Hello")
                                .UpdateKnowledgebase();

                Assert.IsTrue(success);

                IEnumerable<string> answers = maker.GetAnswerStrings();
                Assert.IsTrue(answers.Contains("Hello"));
                IEnumerable<string> questions = maker.GetQuestionsFor("Hello");
                Assert.IsTrue(questions.Contains("Hello"));
                maker.DeleteAnswer("Hello");

                answers = maker.GetAnswerStrings();
                Assert.IsFalse(answers.Contains("Hello"));
                Assert.IsFalse(answers.Any());
            }
            finally
            {
                maker.DeleteKnowledgeBase();
            }
        }

        [TestMethod]
        public void DeleteQuestion()
        {
            QnAMaker maker = GetQnaMaker(nameof(DeleteQuestion));
            try
            {
                QnaUpdateBuilder builder = new QnaUpdateBuilder();
                bool success = builder
                                .Begin(maker)
                                .AddAnswerToQuestions("Hello", new string[] { "There" , "Again"})
                                .UpdateKnowledgebase();

                Assert.IsTrue(success);

                IEnumerable<string> answers = maker.GetAnswerStrings();
                Assert.IsTrue(answers.Contains("Hello"));
                IEnumerable<string> questions = maker.GetQuestionsFor("Hello");
                Assert.IsFalse(questions.Contains("Hello"));
                Assert.IsTrue(questions.Contains("There"));
                Assert.IsTrue(questions.Contains("Again"));

                maker.DeleteQuestion("Hello", "There");
                
                answers = maker.GetAnswerStrings();
                Assert.IsTrue(answers.Contains("Hello"));
                questions = maker.GetQuestionsFor("Hello");
                Assert.IsTrue(questions.Contains("Again"));
                Assert.IsTrue(!questions.Contains("There"));
                Assert.IsTrue(questions.Count() == 1);
            }
            finally
            {
                maker.DeleteKnowledgeBase();
            }
        }
    }
}
