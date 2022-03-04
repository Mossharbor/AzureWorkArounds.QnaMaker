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
        List<QnAKnowledgebase> makers = new List<QnAKnowledgebase>();

        private QnAKnowledgebase GetQnaMaker(string kbToUseForTesting = "qnamakertestkb", bool createIfNotExist = true)
        {
            // note you can see these here: https://www.qnamaker.ai/Home/MyServices
            string ocpApimSubscriptionKey = ConfigurationManager.AppSettings["ocpApimSubscriptionKey"];

            var maker = new QnAKnowledgebase(ConfigurationManager.AppSettings["qnaMakerName"], kbToUseForTesting, ocpApimSubscriptionKey);
            if (createIfNotExist)
                maker.CreateIfDoesntExist();
            makers.Add(maker);
            return maker;
        }

        [TestMethod]
        public void CreateEmptyKB()
        {
            QnAKnowledgebase qna = null;
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
        public void CreateKBThroughBuilder()
        {
            QnAKnowledgebase qna = null;
            try
            {
                QnaKnowledgebaseBuilder builder = new QnaKnowledgebaseBuilder();
                bool success = builder
                                .Create(ConfigurationManager.AppSettings["qnaMakerName"], nameof(CreateKBThroughBuilder), ConfigurationManager.AppSettings["ocpApimSubscriptionKey"])
                                .UpdateKnowledgebase();

                qna = builder.Knowledgebase;
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
                QnaKnowledgebaseBuilder builder = new QnaKnowledgebaseBuilder();
                bool success = builder
                                .Modify(qna)
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
                QnaKnowledgebaseBuilder builder = new QnaKnowledgebaseBuilder();
                bool success = builder
                                .Modify(qna)
                                .AddAnswerToQuestion("Sunny", "What is the weather like?")
                                .UpdateKnowledgebase();

                Assert.IsTrue(success);

                // qna.Train();
                qna.Publish();

                success = builder
                                .Modify(qna)
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
            QnAKnowledgebase maker = GetQnaMaker(nameof(AddQuestionAndAnswer));
            try
            {
                QnaKnowledgebaseBuilder builder = new QnaKnowledgebaseBuilder();
                bool success = builder
                                .Modify(maker)
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
            QnAKnowledgebase maker = GetQnaMaker(nameof(AddMultipleQuestionAndAnswer));
            Assert.IsTrue(maker.GetDetails() != null);
            try
            {
                QnaKnowledgebaseBuilder builder = new QnaKnowledgebaseBuilder();
                bool success = builder
                                .Modify(maker)
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
            QnAKnowledgebase maker = GetQnaMaker(nameof(UpdateExistingAnswerWithAdditionalQuestions));
            try
            {
                QnaKnowledgebaseBuilder builder = new QnaKnowledgebaseBuilder();
                bool success = builder
                                .Modify(maker)
                                .AddAnswerToQuestions("Hello", new string[] { "Hello"})
                                .UpdateKnowledgebase();

                Assert.IsTrue(success);

                IEnumerable<string> answers = maker.GetAnswerStrings();
                Assert.IsTrue(answers.Contains("Hello"));

                IEnumerable<string> questions = maker.GetQuestionsFor("Hello");
                Assert.IsTrue(questions.Contains("Hello"));
                Assert.IsFalse(questions.Contains("There"));

                success = builder
                                .Modify(maker)
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
            QnAKnowledgebase maker = GetQnaMaker(nameof(DeleteAnswer));
            try
            {
                QnaKnowledgebaseBuilder builder = new QnaKnowledgebaseBuilder();
                bool success = builder
                                .Modify(maker)
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
            QnAKnowledgebase maker = GetQnaMaker(nameof(DeleteQuestion));
            try
            {
                QnaKnowledgebaseBuilder builder = new QnaKnowledgebaseBuilder();
                bool success = builder
                                .Modify(maker)
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
