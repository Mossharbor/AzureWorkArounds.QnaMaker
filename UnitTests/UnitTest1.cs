using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    using Mossharbor.AzureWorkArounds.QnaMaker;
    using System.Collections.Generic;

    [TestClass]
    public class QnaMakerUnitTests
    {
        private QnAMaker GetQnaMaker()
        {
            string kb = "";
            string subscriptionKey = "EndpointKey ....";
            string ocpApimSubscriptionKey = "";

            // TODO enter you credentials in here!!
            return new QnAMaker("mixedrealityheadsetqna", kb, subscriptionKey, ocpApimSubscriptionKey);
        }

        [TestMethod]
        public  void GetAnswer()
        {
            var qna = GetQnaMaker();
            var answers = qna.GenerateAnswer("hi");
        }

        [TestMethod]
        public void AddQuestionAndAnswer()
        {
            QnAMaker maker = GetQnaMaker();
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
                maker.DeleteAnswer("Hello");
            }
        }

        [TestMethod]
        public void AddMultipleQuestionAndAnswer()
        {
            QnAMaker maker = GetQnaMaker();
            try
            {
                QnaUpdateBuilder builder = new QnaUpdateBuilder();
                bool success = builder
                                .Begin(maker)
                                .AddQuestionsAndAnswer("Hello", new string[] { "Hello", "There" })
                                .Update();

                Assert.IsTrue(success);

                List<string> answers = maker.GetAnswerStrings();
                Assert.IsTrue(answers.Contains("Hello"));

                List<string> questions = maker.GetQuestionsFor("Hello");
                Assert.IsTrue(questions.Contains("Hello"));
                Assert.IsTrue(questions.Contains("There"));
            }
            finally
            {
                maker.DeleteAnswer("Hello");
            }
        }

        [TestMethod]
        public void UpdateExistingAnswer()
        {
            QnAMaker maker = GetQnaMaker();
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
                maker.DeleteAnswer("Hello");
            }
        }

        [TestMethod]
        public void RemoveAnswer()
        {
            QnAMaker maker = GetQnaMaker();
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
            }
        }

        [TestMethod]
        public void RemoveQuestion()
        {
            QnAMaker maker = GetQnaMaker();
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
                Assert.IsTrue(questions.Contains("Hello"));
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
                maker.DeleteAnswer("Hello");
            }
        }
    }
}
