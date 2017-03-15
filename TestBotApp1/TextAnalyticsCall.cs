using System.Collections.Generic;

namespace TextSentiBot
{
    // Classes to store the input for the sentiment API call
    public class BatchInput
    {
        public List<DocumentInput> documents { get; set; }
    }
    public class DocumentInput
    {
        public double id { get; set; }
        public string text { get; set; }
    }

    // Classes to store the result from the sentiment analysis
    public class BatchResult
    {
        public List<DocumentResult> documents { get; set; }
    }
    public class DocumentResult
    {
        public string id { get; set; }

        public string[] keyPhrases { get; set; }
    }

}