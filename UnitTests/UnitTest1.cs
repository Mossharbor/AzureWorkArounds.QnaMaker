using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    using Mossharbor.AzureWorkArounds.QnaMaker;
    using System.Collections.Generic;
    using System.Configuration;

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
            try
            {
                var qna = GetQnaMaker(nameof(GetAnswer));
                var answers = qna.GenerateAnswer("hi");
            }
            finally
            {
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
                                .AddQuestionAndAnswer("Hello", "Hello")
                                .Update();

                Assert.IsTrue(success);

                List<string> answers = maker.GetAnswerStrings();
                Assert.IsTrue(answers.Contains("Hello"));
                List<string> questions = maker.GetQuestionsFor("Hello");
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
                                .AddQuestionsAndAnswer("Hello", new string[] { "Hello", "There" })
                                .Update();

                Assert.IsTrue(success);
                System.Threading.Thread.Sleep(1000);

                List<string> answers = maker.GetAnswerStrings();
                Assert.IsTrue(answers.Contains("Hello"));

                List<string> questions = maker.GetQuestionsFor("Hello");
                Assert.IsTrue(questions.Contains("Hello"));
                Assert.IsTrue(questions.Contains("There"));
            }
            finally
            {
                maker.DeleteKnowledgeBase();
            }
        }

        [TestMethod]
        public void UpdateExistingAnswer()
        {
            QnAMaker maker = GetQnaMaker(nameof(UpdateExistingAnswer));
            try
            {
                QnaUpdateBuilder builder = new QnaUpdateBuilder();
                bool success = builder
                                .Begin(maker)
                                .AddQuestionsAndAnswer("Hello", new string[] { "Hello"})
                                .Update();

                Assert.IsTrue(success);

                List<string> answers = maker.GetAnswerStrings();
                Assert.IsTrue(answers.Contains("Hello"));

                List<string> questions = maker.GetQuestionsFor("Hello");
                Assert.IsTrue(questions.Contains("Hello"));
                Assert.IsFalse(questions.Contains("There"));

                success = builder
                                .Begin(maker)
                                .AddQuestionsAndAnswer("Hello", new string[] { "There" })
                                .Update();
                Assert.IsTrue(success);
                questions = maker.GetQuestionsFor("Hello");
                Assert.IsTrue(questions.Contains("There"));
            }
            finally
            {
                maker.DeleteKnowledgeBase();
            }
        }

        [TestMethod]
        public void RemoveAnswer()
        {
            QnAMaker maker = GetQnaMaker(nameof(RemoveAnswer));
            try
            {
                QnaUpdateBuilder builder = new QnaUpdateBuilder();
                bool success = builder
                                .Begin(maker)
                                .AddQuestionAndAnswer("Hello", "Hello")
                                .Update();

                Assert.IsTrue(success);

                List<string> answers = maker.GetAnswerStrings();
                Assert.IsTrue(answers.Contains("Hello"));
                List<string> questions = maker.GetQuestionsFor("Hello");
                Assert.IsTrue(questions.Contains("Hello"));
                maker.DeleteAnswer("Hello");

                answers = maker.GetAnswerStrings();
                Assert.IsFalse(answers.Contains("Hello"));
            }
            finally
            {
                maker.DeleteKnowledgeBase();
            }
        }

        [TestMethod]
        public void RemoveQuestion()
        {
            QnAMaker maker = GetQnaMaker(nameof(RemoveQuestion));
            try
            {
                QnaUpdateBuilder builder = new QnaUpdateBuilder();
                bool success = builder
                                .Begin(maker)
                                .AddQuestionsAndAnswer("Hello", new string[] { "There" , "Again"})
                                .Update();

                Assert.IsTrue(success);

                List<string> answers = maker.GetAnswerStrings();
                Assert.IsTrue(answers.Contains("Hello"));
                List<string> questions = maker.GetQuestionsFor("Hello");
                Assert.IsFalse(questions.Contains("Hello"));
                Assert.IsTrue(questions.Contains("There"));
                Assert.IsTrue(questions.Contains("Again"));

                maker.DeleteQuestion("Hello", "There");
                
                answers = maker.GetAnswerStrings();
                Assert.IsTrue(answers.Contains("Hello"));
                questions = maker.GetQuestionsFor("Hello");
                Assert.IsTrue(questions.Contains("Again"));
                Assert.IsTrue(!questions.Contains("There"));
            }
            finally
            {
                maker.DeleteKnowledgeBase();
            }
        }
    }
}
