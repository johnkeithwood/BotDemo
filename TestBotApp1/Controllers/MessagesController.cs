using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System.Net.Http.Headers;
using TextSentiBot;
using Priaid.Diagnosis.Client;
using System.Configuration;
using Priaid.Diagnosis.Client.Model;

namespace TestBotApp1
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

            if (activity == null || activity.GetActivityType() != ActivityTypes.Message)
            {
                //add code to handle errors, or non-messaging activities
            }

            const string apiKey = "674880b753de4d16b4ce1af12790392b";
            string queryUri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/keyPhrases";

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            BatchInput phraseInput = new BatchInput();

            phraseInput.documents = new List<DocumentInput>();
            phraseInput.documents.Add(new DocumentInput()
            {
                id = 1,
                text = activity.Text
            });

            var phraseJsonInput = JsonConvert.SerializeObject(phraseInput);
            byte[] byteData = Encoding.UTF8.GetBytes(phraseJsonInput);
            var content = new ByteArrayContent(byteData);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var phrasePost = await client.PostAsync(queryUri, content);
            var phraseRawResponse = await phrasePost.Content.ReadAsStringAsync();
            var phraseJsonResponse = JsonConvert.DeserializeObject<BatchResult>(phraseRawResponse);
            string[] keyPhrases = phraseJsonResponse.documents[0].keyPhrases;

            var diseases = GetDiseases(keyPhrases);


            var replyMessage = activity.CreateReply();
            replyMessage.Recipient = activity.From;
            replyMessage.Type = ActivityTypes.Message;

            if (diseases.Count > 0)
            {
                replyMessage.Text = "I think you might have ";
                foreach (var disease in diseases)
                {
                    if (replyMessage.Text != "I think you might have ")
                    {
                        replyMessage.Text += " or ";
                    }
                    replyMessage.Text += disease.Issue.Name;
                    
                }
            }
            else
            {
                replyMessage.Text = "Your symptoms don't match any known diseases.";
            }

            //if (sentimentScore > 0.9)
            //{
            //    replyMessage.Text = $"This appears quite positive to me.";
            //}
            //else if (sentimentScore < 0.1)
            //{
            //    replyMessage.Text = $"This appears quite negative to me.";
            //}
            //else
            //{
            //    replyMessage.Text = $"I can not decipher the sentiment here.  Please try again or add more information.";
            //}

            await connector.Conversations.ReplyToActivityAsync(replyMessage);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }

        private List<HealthDiagnosis> GetDiseases(string[] symptoms)
        {

            string username = ConfigurationManager.AppSettings["username"];
            string password = ConfigurationManager.AppSettings["password"];
            string authUrl = ConfigurationManager.AppSettings["priaid_authservice_url"];
            string healthUrl = ConfigurationManager.AppSettings["priaid_healthservice_url"];
            string language = ConfigurationManager.AppSettings["language"];

            var _diagnosisClient = new DiagnosisClient(username, password, authUrl, language, healthUrl);

            var allIssues = _diagnosisClient.LoadSymptoms();

            var matchedIssues = new List<HealthItem>();

            foreach (var symtom in symptoms)
            {
                matchedIssues.AddRange(allIssues.Where(x => x.Name.ToLower().Contains(symtom.ToLower())));
            }

            var symtomIds = new List<int>();
            
            foreach (var issue in matchedIssues)
            {
                symtomIds.Add(issue.ID);
               
            }

            var diseases = new List<HealthDiagnosis>();
            if (symtomIds != null && symtomIds.Count > 0)
            {
                diseases = _diagnosisClient.LoadDiagnosis(symtomIds.Take(1).ToList(), Gender.Male, 1977); 
            }
             

            return diseases;
        }
    }
}