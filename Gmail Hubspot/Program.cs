using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Gmail_Hubspot
{
  class Program
  {
    // If modifying these scopes, delete your previously saved credentials
    // at ~/.credentials/gmail-dotnet-quickstart.json
    static string[] Scopes = { GmailService.Scope.GmailReadonly };
    static string ApplicationName = "Gmail Hubspot";

    private static void GetIDs(ref List<string> IDs, GmailService service, DateTime lastRun, string pageToken = null)
    {
      UsersResource.MessagesResource.ListRequest request = service.Users.Messages.List("me");
      //request.Q = "in:inbox after:" + lastRun;
      request.Q = "in:inbox after: 2017/02/01";
      if (pageToken != null)
      {
        request.PageToken = pageToken;
      }
      ListMessagesResponse response = request.Execute();
      // List labels.
      IList<Message> messages = response.Messages;
      //TODO: add check to see if there are more than 1 page
      if (messages != null && messages.Count > 0)
      {
        foreach (Message message in messages)
        {
          IDs.Add(message.Id);
        }
      }

      if (response.NextPageToken != null)
      {
        GetIDs(ref IDs, service, lastRun, response.NextPageToken);
      }
    }
    static void Main(string[] args)
    {
      UserCredential credential;

      using (var stream =
          new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
      {
        string credPath = @"./credentials/gmail-dotnet-quickstart.json";

        credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.Load(stream).Secrets,
            Scopes,
            "user",
            CancellationToken.None,
            new FileDataStore(credPath, true)).Result;
        Console.WriteLine("Credential file saved to: " + credPath);
      }

      // Create Gmail API service.
      var service = new GmailService(new BaseClientService.Initializer()
      {
        HttpClientInitializer = credential,
        ApplicationName = ApplicationName,
      });
      string[] lines = System.IO.File.ReadAllLines(@"./runtime.txt");
      DateTime lastRun = new DateTime();
      if (lines.Length > 0)
      {
        DateTime.TryParse(lines[lines.Length - 1], out lastRun);
      }
      System.IO.StreamWriter file = new System.IO.StreamWriter(@"./runtime.txt");
      file.WriteLine(DateTime.UtcNow.ToString());
      file.Close();
      List<string> IDs = new List<string>();
      GetIDs(ref IDs, service, lastRun);
      Console.WriteLine(IDs.Count);
      Console.Read();

    }
  }
}
