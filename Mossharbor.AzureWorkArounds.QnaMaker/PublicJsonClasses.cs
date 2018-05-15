using System;
using System.Collections.Generic;
using System.Text;

namespace Mossharbor.AzureWorkArounds.QnaMaker
{
    public class Answer
    {
        public string[] questions { get; set; }
        public string answer { get; set; }
        public float score { get; set; }
        public int id { get; set; }
        public string source { get; set; }
        public object[] metadata { get; set; }
    }

    public class UpdateRootobject
    {
        public Mossharbor.AzureWorkArounds.QnaMaker.Json.Add add { get; set; }
        public Mossharbor.AzureWorkArounds.QnaMaker.Json.Delete delete { get; set; }
        public Mossharbor.AzureWorkArounds.QnaMaker.Json.Update update { get; set; }
    }

    public class Qnadocument : Mossharbor.AzureWorkArounds.QnaMaker.Json.Qnalist
    {
    }
}
