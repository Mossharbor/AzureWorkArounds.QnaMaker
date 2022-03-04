using System;
using System.Collections.Generic;
using System.Text;

namespace Mossharbor.AzureWorkArounds.QnaMaker.Json
{
    using Mossharbor.AzureWorkArounds.QnaMaker;

    public class CreateKnowledgebaseRootobject
    {
        public string name { get; set; }
        public Qnalist[] qnAList { get; set; }
        public string[] urls { get; set; }
        public File[] files { get; set; }
    }

    public class KnowledgeBaseCreationDetailsRootObject
    {
        public string operationState { get; set; }
        public DateTime createdTimestamp { get; set; }
        public DateTime lastActionTimestamp { get; set; }
        public string userId { get; set; }
        public string operationId { get; set; }
    }

    public class KnowledgeBaseListAllRootObject
    {
        public KnowledgeBaseDetails[] knowledgebases { get; set; }
    }

    public class KnowledgeBaseDetails
    {
        public string id { get; set; }
        public string hostName { get; set; }
        public DateTime lastAccessedTimestamp { get; set; }
        public DateTime lastChangedTimestamp { get; set; }
        public DateTime lastPublishedTimestamp { get; set; }
        public string name { get; set; }
        public string userId { get; set; }
        public string language { get; set; }
        public string enableHierarchicalExtraction { get; set; }
        public DateTime createdTimestamp { get; set; }
        public string[] urls { get; set; }
        public string[] sources { get; set; }
        public string[] docSearchSources{ get; set; }
    }

    public class ReplaceRootobject
    {
        public Qnalist[] qnAList { get; set; }
    }

    public class GetAnswersRootObject
    {
        public Answer[] answers { get; set; }
        public Error error { get; set; }
    }
    
    internal class KeysRoot
    {
        public string primaryEndpointKey { get; set; }
        public string secondaryEndpointKey { get; set; }
        public string installedVersion { get; set; }
        public string lastStableVersion { get; set; }
        public Error error { get; set; }
    }

    public class Error
    {
        public string code { get; set; }
        public string message { get; set; }
    }

    public class Add
    {
        public Qnalist[] qnaList { get; set; }
        public string[] urls { get; set; }
        public File[] files { get; set; }
    }

    public class Qnalist
    {
        public int id { get; set; }
        public string answer { get; set; }
        public string source { get; set; }
        public string[] questions { get; set; }
        public Metadata[] metadata { get; set; }
    }

    [Newtonsoft.Json.JsonObject("qnalist")]
    public class UpdateQnaList : Qnalist
    {
        public UpdateQnaList() { questions = new UpateQuestions(); }
        public new UpateQuestions questions{get;set;}
    }

    [Newtonsoft.Json.JsonObject("questions")]
    public class UpateQuestions
    {
        public UpateQuestions() { this.add = new string[0]; this.delete = new string[0]; }
        public string[] add { get; set; }
        public string[] delete { get; set; }
    }

    public class Metadata
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class File
    {
        public string fileName { get; set; }
        public string fileUri { get; set; }
    }

    public class Delete
    {
        public int[] ids { get; set; }
        public string[] sources { get; set; }
    }

    public class Update
    {
        public string name { get; set; }
        public Qnalist[] qnaList { get; set; }
        public string[] urls { get; set; }
    }

    public class Questions
    {
        public string[] add { get; set; }
        public string[] delete { get; set; }
    }


    public class KnowledgeBaseRootobject
    {
        public Qnadocument[] qnaDocuments { get; set; }
    }


}
