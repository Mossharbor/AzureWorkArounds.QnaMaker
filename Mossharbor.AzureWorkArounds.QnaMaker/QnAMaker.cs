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

    public enum EnvironmentType { Test, Prod}

    /// <summary>
    /// This class directly interacts with the QnaMakers rest api
    /// </summary>
    public class QnAMaker
    {
        static string baseUrl = "cognitiveservices.azure.com";
        string knowledgebase;
        string key;
        string ocpApimSubscriptionKey;
        string azureServicName;
        string endpoint;
        string knowledgeBaseId;
        KnowledgeBaseDetails details;

        /// <summary>
        /// basic constructor
        /// </summary>
        /// <param name="azureServicName">this is the name of your azure service</param>
        /// <param name="knowledgebase">this is your knowledgebase guid</param>
        /// <param name="ocpApimSubscriptionKey">This is your ocpApim subscription key</param>
        /// <param name="region">the region westus by default</param>
        /// <remarks> we load the endpoint key if we need to using the ocpApimSubscriptionKey</remarks>
        public QnAMaker(string azureServicName, string knowledgebase, string ocpApimSubscriptionKey, string region = "westus")
        {
            this.knowledgebase = knowledgebase;
            this.ocpApimSubscriptionKey = ocpApimSubscriptionKey;
            this.azureServicName = azureServicName;
            this.endpoint = region;
        }

        /// <summary>
        /// basic constructor
        /// </summary>
        /// <param name="azureServicName">this is the name of your azure service</param>
        /// <param name="knowledgebase">this is your knowledgebase guid</param>
        /// <param name="endpointKey">This is your endpoint key</param>
        /// <param name="ocpApimSubscriptionKey">This is your ocpApim subscription key</param>
        /// <param name="region">the region westus by default</param>
        public QnAMaker(string azureServicName, string knowledgebase, string endpointKey, string ocpApimSubscriptionKey, string region = "westus")
        {
            this.knowledgebase = knowledgebase;
            this.key = endpointKey;
            this.ocpApimSubscriptionKey = ocpApimSubscriptionKey;
            this.azureServicName = azureServicName;
            this.endpoint = region;
        }

        /// <summary>
        /// basic constructor
        /// </summary>
        /// <param name="azureServicName">this is the name of your azure service</param>
        /// <param name="knowledgebase">this is your knowledgebase guid</param>
        /// <param name="kowledgebaseId">the id of the knowledgebase (found the POST url after publish POST /knowledgebases/b0a466fc-54bc-476d-a1c3-af10d4c974a2/generateAnswer)</param>
        /// <param name="ocpApimSubscriptionKey">This is your ocpApim subscription key</param>
        /// <param name="region">the region westus by default</param>
        /// <remarks> we load the endpoint key if we need to using the ocpApimSubscriptionKey</remarks>
        public QnAMaker(string azureServicName, string knowledgebase, Guid kowledgebaseId, string ocpApimSubscriptionKey, string region = "westus")
        {
            this.knowledgebase = knowledgebase;
            this.knowledgeBaseId = kowledgebaseId.ToString();
            this.ocpApimSubscriptionKey = ocpApimSubscriptionKey;
            this.azureServicName = azureServicName;
            this.endpoint = region;
        }

        /// <summary>
        /// basic constructor
        /// </summary>
        /// <param name="azureServicName">this is the name of your azure service</param>
        /// <param name="knowledgebase">this is your knowledgebase guid</param>
        /// <param name="kowledgebaseId">the id of the knowledgebase (found the POST url after publish POST /knowledgebases/b0a466fc-54bc-476d-a1c3-af10d4c974a2/generateAnswer)</param>
        /// <param name="endpointKey">This is your endpoint key</param>
        /// <param name="ocpApimSubscriptionKey">This is your ocpApim subscription key</param>
        /// <param name="region">the region westus by default</param>
        public QnAMaker(string azureServicName, string knowledgebase, Guid kowledgebaseId, string endpointKey, string ocpApimSubscriptionKey, string region = "westus")
        {
            this.knowledgebase = knowledgebase;
            this.knowledgeBaseId = kowledgebaseId.ToString();
            this.key = endpointKey;
            this.ocpApimSubscriptionKey = ocpApimSubscriptionKey;
            this.azureServicName = azureServicName;
            this.endpoint = region;
        }

        private string GetPrimaryEndpointKey(string qnaMakerResourceName)
        {
            string RequestURI = String.Format(@" https://{0}.{1}/qnamaker/v4.0/endpointkeys", this.azureServicName, baseUrl);
            using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ocpApimSubscriptionKey);
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, RequestURI);

                HttpResponseMessage msg = client.SendAsync(requestMessage).Result;
                var jsonResponse = msg.Content.ReadAsStringAsync().Result;
                KeysRoot keysJson = JsonConvert.DeserializeObject<KeysRoot>(jsonResponse);
                if (null != keysJson.error)
                    throw new Exception(keysJson.error.message);

                return keysJson.primaryEndpointKey;
            }
        }

        public void CreateKnowledgeBaseIfDoesntExist()
        {
            if (!this.KnowledgeBaseExists())
            {
                this.CreateKnowledgeBase(this.knowledgebase);
                this.details = this.GetDetails(); 
            }

            this.knowledgeBaseId = this.details?.id;
        }

        private IEnumerable<Qnadocument> kbData = null;

        public KnowledgeBaseDetails GetDetails()
        {
            if (null != this.details)
                return this.details;
            
            using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
            {
                string RequestURI = $"https://{this.azureServicName}.{baseUrl}/qnamaker/v4.0/knowledgebases/";

                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ocpApimSubscriptionKey);

                System.Net.Http.HttpResponseMessage msg = client.GetAsync(RequestURI).Result;

                msg.EnsureSuccessStatusCode();

                var JsonDataResponse = msg.Content.ReadAsStringAsync().Result;

                KnowledgeBaseListAllRootObject allKBs = JsonConvert.DeserializeObject<KnowledgeBaseListAllRootObject>(JsonDataResponse);

                if (null == allKBs)
                    throw new JsonException("Could not deserialize the list of knowledgebases");

                if (!string.IsNullOrEmpty(this.knowledgeBaseId))
                {
                    this.details = allKBs.knowledgebases.FirstOrDefault(p => p.id == this.knowledgeBaseId);
                    return this.details;
                }

                var allDetails = allKBs.knowledgebases.Where(p => p.name == this.knowledgebase).ToArray();
                if (allDetails.Length == 0)
                    return null;
                if (allDetails.Length > 1)
                    throw new KeyNotFoundException($"More than one Knowledge base found with name {this.knowledgebase}, please pass in knowledge base id to differentiate them");

                this.details = allDetails[0];
                return this.details;
            }
        }

        private bool KnowledgeBaseExists()
        {
            KnowledgeBaseDetails details = GetDetails();
            if (null != details)
                this.details = details;
            return (null != details);
        }

        /// <summary>
        /// Return the list of questions for this answer
        /// </summary>
        /// <param name="answer">the answer we need to get the list of questions for</param>
        /// <param name="comparison"></param>
        /// <returns>a list of questions</returns>
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

        /// <summary>
        /// Returns All of the answers in the KB
        /// </summary>
        /// <returns>All of the answers in the KB</returns>
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

        /// <summary>
        /// Returns a list of all the answers that are valid for a specifc question
        /// </summary>
        /// <param name="question">a question we woudl like to find the answers to</param>
        /// <param name="comparison"></param>
        /// <returns>a list of all the answers that are valid for a specifc question</returns>
        public List<Answer> GetAnswersWith(string question, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            List<Answer> answers = new List<Answer>();

            foreach (var t in KBData)
            {
                if (null == t.answer)
                    continue;
                var found = t.questions.FirstOrDefault(p => p.Equals(question, comparison));

                if (String.IsNullOrEmpty(found))
                    answers.Add(new Answer() { id =t.id, answer = t.answer, metadata = t.metadata, questions = t.questions, source = t.source, score = 0 });
            }

            return answers;
        }

        /// <summary>
        /// Gets the full list of answers in the DB
        /// </summary>
        /// <returns>the full list of answers in the DB</returns>
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

        /// <summary>
        /// This is a full list of all the data in the kownledge base
        /// </summary>
        public  IEnumerable<Qnadocument> KBData
        {
            get
            {
                if (null == kbData || kbData.Any())
                    kbData = GetKnowledgebaseData();
                return kbData;
            }
        }

        /// <summary>
        /// Indicates that we need to redownload the kb
        /// </summary>
        internal void Reset()
        {
            this.kbData = null;
        }

        internal bool Update(UpdateRootobject toPublish)
        {
            if (null == toPublish.add?.qnaList && null == toPublish.delete?.ids && null == toPublish.update?.qnaList)
                return false;

            if (null == this.details)
            {
                this.details = GetDetails();
                this.knowledgeBaseId = this.details.id;
            }
            
            string host = $"https://{this.azureServicName}.{baseUrl}";
            string service = "/qnamaker/v4.0";
            string method = "/knowledgebases/{0}";
            var method_with_id = String.Format(method, this.knowledgeBaseId);
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
                WaitForOperationToComplete(client, response);

                return response.IsSuccessStatusCode;
            }
        }

        private void WaitForOperationToComplete(HttpClient client, HttpResponseMessage response)
        {
            string operationResult = response.Content.ReadAsStringAsync().Result;
            string original = operationResult;
            var operationDetails = JsonConvert.DeserializeObject<KnowledgeBaseOperationDetailsRootObject>(operationResult);

            // todo add timespan check here.
            while (operationDetails.operationState == "NotStarted" || operationDetails.operationState == "Running")
            {
                System.Threading.Thread.Sleep(1000);
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ocpApimSubscriptionKey);

                using (var operationRequest = new HttpRequestMessage())
                {
                    operationRequest.Method = new HttpMethod("GET");
                    operationRequest.RequestUri = new Uri($"https://{this.azureServicName}.{baseUrl}/qnamaker/v4.0/operations/{operationDetails.operationId}");

                    var operationResponse = client.SendAsync(operationRequest).Result;
                    operationResult = operationResponse.Content.ReadAsStringAsync().Result;
                    operationDetails = JsonConvert.DeserializeObject<KnowledgeBaseOperationDetailsRootObject>(operationResult);
                }
            }

            if (operationDetails.operationState == "Failed")
                throw new Exception("Failed operation");
        }

        private static T[] Combine<T>(T[] oldArray, T[] items)
        {
            T[] newArray = new T[oldArray.Length + items.Length];
            Array.Copy(oldArray, newArray, oldArray.Length);
            Array.Copy(items, 0, newArray, oldArray.Length, items.Length);
            return newArray;
        }

        internal bool Replace(UpdateRootobject toPublish)
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

            string host = $"https://{this.endpoint}.api.cognitive.microsoft.com";
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

        /// <summary>
        /// Returns a list of questions for a specific answer
        /// </summary>
        /// <param name="answer">the answer we are trying to compare against</param>
        /// <returns>a list of questions for a specific answer</returns>
        public string[] GetQuestionsForAnswer(string answer)
        {
            foreach (Qnadocument doc in KBData)
            {
                if (doc.answer.Equals(answer, StringComparison.OrdinalIgnoreCase))
                    return doc.questions;
            }
            return new string[0];
        }

        /// <summary>
        /// Get the specific answer id in the kb
        /// </summary>
        /// <param name="answer">The answer we are looking for</param>
        /// <returns>the specific answer id in the kb</returns>
        public int GetAnswerID(string answer)
        {
            foreach(Qnadocument doc in KBData)
            {
                if (doc.answer.Equals(answer, StringComparison.OrdinalIgnoreCase))
                    return doc.id;
            }

            return -1;
        }

        /// <summary>
        /// Delete the answer out of a db
        /// </summary>
        /// <param name="answer">the answer to delete</param>
        /// <returns>true if the rest call returns success</returns>
        public bool DeleteAnswer(string answer)
        {
            QnaUpdateBuilder builder = new QnaUpdateBuilder();
            return builder.Begin(this).RemoveAnswer(answer).UpdateKnowledgebase();
        }

        /// <summary>
        /// Deletes any question that matches out of the db
        /// </summary>
        /// <param name="question">The question we are trying to delete</param>
        /// <returns>true if the rest call returns success</returns>
        public bool DeleteQuestion(string question)
        {
            QnaUpdateBuilder builder = new QnaUpdateBuilder();
            return builder.Begin(this).RemoveQuestion(question).UpdateKnowledgebase();
        }

        /// <summary>
        /// Delete a question for a specific answer
        /// </summary>
        /// <param name="answer">The answer we are tring to match</param>
        /// <param name="question">The question we are trying to answer</param>
        /// <returns>true if the rest call returns success</returns>
        public bool DeleteQuestion(string answer, string question)
        {
            QnaUpdateBuilder builder = new QnaUpdateBuilder();
            return builder.Begin(this).RemoveQuestion(answer, question).UpdateKnowledgebase();
        }

        /// <summary>
        /// Delete a list of questions from a specific answer in the db
        /// </summary>
        /// <param name="answer">The answer we are looking for</param>
        /// <param name="questions">The question list we are trying to remove</param>
        /// <returns></returns>
        public bool DeleteQuestions(string answer, string[] questions)
        {
            QnaUpdateBuilder builder = new QnaUpdateBuilder();
            return builder.Begin(this).RemoveQuestions(answer, questions).UpdateKnowledgebase();
        }

        /// <summary>
        /// Call the Knowledge base and get it to return an answer
        /// </summary>
        /// <param name="question">The question we would like to run through the kb</param>
        /// <param name="env">the environment we are going to query</param>
        /// <param name="count">the number of answers we would like to return (3 is default)</param>
        /// <param name="scorethreshold">the threshold in which to accept something as an answer</param>
        /// <returns>a list of answers</returns>
        public IEnumerable<Answer> GenerateAnswer(string question, EnvironmentType env = EnvironmentType.Test, int count = 3, int scorethreshold = 30)
        {
            if (null == this.key)
            {
                this.key = GetPrimaryEndpointKey(ocpApimSubscriptionKey);
            }

            //Create my object
            var questionObject = new
            {
                question = question,
                top = count,
                isTest = env == EnvironmentType.Test ? "true" : "false",
                scoreThreshold = scorethreshold,
            };


            string questionJson = JsonConvert.SerializeObject(questionObject);
            string RequestURI = String.Format("{0}{1}{2}", @"https://"+ azureServicName+".azurewebsites.net/qnamaker/knowledgebases/", this.knowledgeBaseId, @"/generateAnswer");
            using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
            {
                //client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ocpApimSubscriptionKey);
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, RequestURI);
                requestMessage.Content = new StringContent(questionJson,Encoding.UTF8, "application/json");
                requestMessage.Headers.Add("Authorization", this.key);

                HttpResponseMessage msg = client.SendAsync(requestMessage).Result;
                var jsonResponse = msg.Content.ReadAsStringAsync().Result;
                GetAnswersRootObject answersjson = JsonConvert.DeserializeObject<GetAnswersRootObject>(jsonResponse);
                if (null != answersjson.error)
                    throw new Exception(answersjson.error.message);

                return answersjson.answers;
            }
        }

        /// <summary>
        /// Creates an empty knowledge base
        /// </summary>
        /// <param name="knowledgeBaseName">the name of the knowledgebase to create</param>
        public void CreateKnowledgeBase(string knowledgeBaseName)
        {
            string host = $"https://{this.endpoint}.api.cognitive.microsoft.com";
            string service = "/qnamaker/v4.0";
            string method = "/knowledgebases/create/";
            var uri = host + service + method;

            var newKbObj = new CreateKnowledgebaseRootobject();
            newKbObj.name = knowledgeBaseName;

            string requestBody= JsonConvert.SerializeObject(newKbObj);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ocpApimSubscriptionKey);

                var response = client.SendAsync(request).Result;
                WaitForOperationToComplete(client, response);
            }
        }

        private IEnumerable<Qnadocument> GetKnowledgebaseData(EnvironmentType env = EnvironmentType.Test)
        {
            var response = GetKnowledgebaseJson(env);
            return JsonConvert.DeserializeObject<KnowledgeBaseRootobject>(response).qnaDocuments;
        }

        public string GetKnowledgebaseJson(EnvironmentType env = EnvironmentType.Test)
        {
            if (null == this.details)
            {
                this.details = GetDetails();
                this.knowledgeBaseId = this.details.id;
            }

            // Make a web call to get the contents of the
            // .tsv file that contains the database
            string RequestURI = $"https://{this.azureServicName}.{baseUrl}/qnamaker/v4.0/knowledgebases/{this.knowledgeBaseId}/{env}/qna";
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Get;
                request.RequestUri = new Uri(RequestURI);
                request.Headers.Add("Ocp-Apim-Subscription-Key", this.ocpApimSubscriptionKey);
                var response = client.SendAsync(request).Result;
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        public void DeleteKnowledgeBase()
        {
            if (null == this.details)
            {
                this.details = GetDetails();
                this.knowledgeBaseId = this.details.id;
            }

            string RequestURI = $"https://{this.azureServicName}.{baseUrl}/qnamaker/v4.0/knowledgebases/{this.knowledgeBaseId}";

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Delete;
                request.RequestUri = new Uri(RequestURI);
                request.Headers.Add("Ocp-Apim-Subscription-Key", this.ocpApimSubscriptionKey);
                var response = client.SendAsync(request).Result;
                response.EnsureSuccessStatusCode();
            }
        }

        /// <summary>
        /// this retrieves the specific id for a given question
        /// </summary>
        /// <param name="question">The question we are looking for</param>
        /// <returns>the specific id for a given answer</returns>
        public IEnumerable<int> GetAnswerIDsForQuestion(string question)
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

            return anwerIds;
        }

        public void Train()
        {
            // POST {RuntimeEndpoint}/qnamaker/knowledgebases/{kbId}/train
            string uri = $"https://{this.azureServicName}.{baseUrl}/qnamaker/v5.0-preview.2/knowledgebases/{this.knowledgeBaseId}/train";
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ocpApimSubscriptionKey);

                var response = client.SendAsync(request).Result;

                response.EnsureSuccessStatusCode();

                //response.Content.ReadAsStringAsync().Result
            }
        }

        /// <summary>
        /// Publishes the test environment to production.
        /// </summary>
        public void Publish()
        {
            string uri = $"https://{this.azureServicName}.{baseUrl}/qnamaker/v4.0/knowledgebases/{this.knowledgeBaseId}";
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ocpApimSubscriptionKey);

                var response = client.SendAsync(request).Result;

                response.EnsureSuccessStatusCode();
            }
        }
    }
}
