using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Mossharbor.AzureWorkArounds.QnaMaker
{
    using Mossharbor.AzureWorkArounds.QnaMaker.Json;
    using System.Linq;

    public class QnAMaker
    {
        //static string host = "https://mixedrealityheadsetqna.azurewebsites.net";
        string knowledgebase;
        string key;
        string ocpApimSubscriptionKey;
        string azureServicName;

        public QnAMaker(string azureServicName, string knowledgebase, string key, string ocpApimSubscriptionKey)
        {
            this.knowledgebase = knowledgebase;
            this.key = key;
            this.ocpApimSubscriptionKey = ocpApimSubscriptionKey;
            this.azureServicName = azureServicName;
        }

        private Qnadocument[] kbData = null;

        public List<string> GetQuestionsFor(string answer, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            List<string> questions = new List<string>();

            foreach (var t in KBData)
            {
                if (null == t.answer)
                    continue;

                if (t.answer.Equals(answer, comparison))
                    questions.AddRange(t.questions);
            }

            return questions;
        }

        public List<string> GetAnswerStrings()
        {
            List<string> answers = new List<string>();

            foreach (var t in KBData)
            {
                if (null == t.answer)
                    continue;

                answers.Add(t.answer);
            }

            return answers;
        }

        public List<Answer> GetAnswersWith(string question, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            List<Answer> answers = new List<Answer>();

            foreach (var t in KBData)
            {
                if (null == t.answer)
                    continue;
                var found = t.questions.FirstOrDefault(p => p.Equals(question, comparison);

                if (String.IsNullOrEmpty(found))
                    answers.Add(new Answer() { id =zt.id, answer = t.answer, metadata = t.metadata, questions = t.questions, source = t.source, score = 0 });
            }

            return answers;
        }

        public List<Answer> GetAnswers()
        {
            List<Answer> answers = new List<Answer>();

            foreach(var t in KBData)
            {
                if (null == t.answer)
                    continue;

                answers.Add(new Answer() { id = t.id, answer = t.answer, metadata = t.metadata, questions = t.questions, source = t.source, score = 0 });
            }

            return answers;
        }

        public  Qnadocument[] KBData
        {
            get
            {
                if (null == kbData)
                    kbData = GetKnowledgebaseData();
                return kbData;
            }
        }

        internal void Reset()
        {
            this.kbData = null;
        }

        public void Publish()
        {
            string host = "https://westus.api.cognitive.microsoft.com";
            string service = "/qnamaker/v4.0";
            string method = "/knowledgebases/{0}/";
            var method_kb = String.Format(method, this.knowledgebase);
            var uri = host + service + method_kb;
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ocpApimSubscriptionKey);

                var response = client.SendAsync(request).Result;
            }
        }

        public bool Update(UpdateRootobject toPublish)
        {
            if (null == toPublish.add?.qnaList && null == toPublish.delete?.ids && null == toPublish.update?.qnaList)
                return false;
            
            string host = "https://westus.api.cognitive.microsoft.com";
            string service = "/qnamaker/v4.0";
            string method = "/knowledgebases/{0}";
            var method_with_id = String.Format(method, this.knowledgebase);
            var uri = host + service + method_with_id;
            string requestBody = JsonConvert.SerializeObject(toPublish);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = new HttpMethod("PATCH");
                request.RequestUri = new Uri(uri);
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ocpApimSubscriptionKey);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                var response = client.SendAsync(request).Result;
                return response.IsSuccessStatusCode;
            }
        }

        private static T[] Combine<T>(T[] oldArray, T[] items)
        {
            T[] newArray = new T[oldArray.Length + items.Length];
            Array.Copy(oldArray, newArray, oldArray.Length);
            Array.Copy(items, 0, newArray, oldArray.Length, items.Length);
            return newArray;
        }

        public bool Replace(UpdateRootobject toPublish)
        {
            if (null == toPublish.add?.qnaList && null == toPublish.update?.qnaList)
                return false;

            Qnalist[] list = null;
            if (null != toPublish.add?.qnaList && null != toPublish.update?.qnaList)
                list = Combine(toPublish.add?.qnaList, toPublish.update?.qnaList);
            else if (null != toPublish.add?.qnaList)
                list = toPublish.add.qnaList;
            else if (null != toPublish.update?.qnaList)
                list = toPublish.update.qnaList;

            Add newAdd = new Add();
            newAdd.qnaList = list;

            string host = "https://westus.api.cognitive.microsoft.com";
            string service = "/qnamaker/v4.0";
            string method = "/knowledgebases/{0}";
            var method_with_id = String.Format(method, this.knowledgebase);
            var uri = host + service + method_with_id;
            string requestBody = JsonConvert.SerializeObject(newAdd);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = new HttpMethod("PUT");
                request.RequestUri = new Uri(uri);
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ocpApimSubscriptionKey);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                var response = client.SendAsync(request).Result;
                return response.IsSuccessStatusCode;
            }
        }

        public string[] GetQuestionsForAnswer(string answer)
        {
            foreach (Qnadocument doc in KBData)
            {
                if (doc.answer.Equals(answer, StringComparison.OrdinalIgnoreCase))
                    return doc.questions;
            }
            return new string[0];
        }

        public int GetAnswerID(string answer)
        {
            foreach(Qnadocument doc in KBData)
            {
                if (doc.answer.Equals(answer, StringComparison.OrdinalIgnoreCase))
                    return doc.id;
            }

            return -1;
        }

        public bool DeleteAnswer(string answer)
        {
            QnaUpdateBuilder builder = new QnaUpdateBuilder();
            return builder.Begin(this).RemoveAnswer(answer).Update();
        }

        public bool DeleteQuestion(string question)
        {
            QnaUpdateBuilder builder = new QnaUpdateBuilder();
            return builder.Begin(this).RemoveQuestion(question).Update();
        }

        public bool DeleteQuestion(string answer, string question)
        {
            QnaUpdateBuilder builder = new QnaUpdateBuilder();
            return builder.Begin(this).RemoveQuestion(answer, question).Update();
        }

        public bool DeleteQuestions(string answer, string[] questions)
        {
            QnaUpdateBuilder builder = new QnaUpdateBuilder();
            return builder.Begin(this).RemoveQuestion(answer, questions).Update();
        }

        public Answer[] GenerateAnswer(string question, int count = 3)
        {
            string questionJson = "{ \"question\":\""+ question + "\", \"top\":\""+ count + "\"";
            string RequestURI = String.Format("{0}{1}{2}", @"https://"+ azureServicName+".azurewebsites.net/qnamaker/knowledgebases/", this.knowledgebase, @"/generateAnswer");
            using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
            {
                //client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ocpApimSubscriptionKey);
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, RequestURI);
                requestMessage.Content = new StringContent(questionJson,Encoding.UTF8, "application/json");
                client.DefaultRequestHeaders.Add("Authorization", this.key);

                HttpResponseMessage msg = client.SendAsync(requestMessage).Result;
                var jsonResponse = msg.Content.ReadAsStringAsync().Result;
                GetAnswersRootObject answersjson = JsonConvert.DeserializeObject<GetAnswersRootObject>(jsonResponse);
                if (null == answersjson.error)
                    throw new Exception(answersjson.error.message);

                return answersjson.answers;
            }
        }

        public Qnadocument[] GetKnowledgebaseData()
        {
            string host = "https://westus.api.cognitive.microsoft.com";
            string service = "/qnamaker/v4.0";
            string method = "/knowledgebases/{0}/{1}/qna/";
            string env = "Test";
            var method_with_id = String.Format(method, this.knowledgebase, env);
            var uri = host + service + method_with_id;

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Get;
                request.RequestUri = new Uri(uri);
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ocpApimSubscriptionKey);

                var response = client.SendAsync(request).Result;
                var t =  JsonConvert.DeserializeObject<KnowledgeBaseRootobject>(response.Content.ReadAsStringAsync().Result).qnaDocuments;
                if (null == kbData)
                    kbData = t;
                return t;
            }

        }

        public static string GetKnowledgebaseAsString(string knowledgeBase, string ocpApimSubscriptionKey)
        {
            string strFAQUrl = String.Empty;
            string strLine;
            StringBuilder sb = new StringBuilder();

            using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
            {
                string RequestURI = String.Format("{0}{1}{2}", @"https://westus.api.cognitive.microsoft.com/qnamaker/v2.0/knowledgebases/", knowledgeBase, @"? ");

                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ocpApimSubscriptionKey);

                System.Net.Http.HttpResponseMessage msg = client.GetAsync(RequestURI).Result;

                if (msg.IsSuccessStatusCode)
                {
                    var JsonDataResponse = msg.Content.ReadAsStringAsync().Result;

                    strFAQUrl = JsonConvert.DeserializeObject<string>(JsonDataResponse);
                }
            }

            // Make a web call to get the contents of the
            // .tsv file that contains the database
            var req = WebRequest.Create(strFAQUrl);
            var r = req.GetResponseAsync().Result;

            // Read the response
            using (var responseReader = new StreamReader(r.GetResponseStream()))
            {
                // Read through each line of the response
                while ((strLine = responseReader.ReadLine()) != null)
                {
                    // Write the contents to the StringBuilder object
                    string[] strCurrentLine = strLine.Split('\t');
                    sb.Append((String.Format("{0},{1},{2}\n",strCurrentLine[0],strCurrentLine[1],strCurrentLine[2])));
                }
            }

            // Return the contents of the StringBuilder object
            return sb.ToString();

        }
        public int[] GetAnswerIDsForQuestion(string question)
        {
            List<int> anwerIds = new List<int>();
            foreach (var t in KBData)
            {
                foreach(var q in t.questions)
                {
                    if (q.Equals(question, StringComparison.OrdinalIgnoreCase))
                        anwerIds.Add(t.id);
                }
            }

            return anwerIds.ToArray();
        }
    }
}
