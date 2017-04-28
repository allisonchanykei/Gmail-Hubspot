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

    private static void GetIDs(ref List<string> IDs, GmailService service, int lastRun, string pageToken = null)
    {
      UsersResource.MessagesResource.ListRequest request = service.Users.Messages.List("me");
      //request.Q = "category:primary after:" + lastRun;
      request.Q = "category:primary after: 2017/02/01";
      if (pageToken != null)
      {
        request.PageToken = pageToken;
      }
      ListMessagesResponse response = request.Execute();
      // List labels.
      IList<Message> messages = response.Messages;

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

    private static List<string> GetEmails(List<string> IDs, GmailService service)
    {
      List<string> Senders = new List<string>();
      foreach (string ID in IDs)
      {
        UsersResource.MessagesResource.GetRequest request = service.Users.Messages.Get("me", ID);
        request.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Metadata;
        request.MetadataHeaders = "from";
        Message response = request.Execute();
        //Creating a List of Senders Emails
        foreach (MessagePartHeader header in response.Payload.Headers)
        {
          Console.WriteLine(header.Value);
          Senders.Add(header.Value);
        }
      }
      return Senders;
    }
    static void Main(string[] args)
    {
      UserCredential credential;

      using (var stream =
          new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
      {
        string credPath = "./credentials/gmail-hubspot.json";

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
      string runTimeFile = "./runtime.txt";
      if (!File.Exists(runTimeFile))
      {
        File.Create(runTimeFile);
      }

      string[] lines = File.ReadAllLines(runTimeFile);

      DateTime lastRun = new DateTime(2000, 1, 1);
      if (lines.Length > 0)
      {
        DateTime.TryParse(lines[lines.Length - 1], out lastRun);
      }
      StreamWriter file = new StreamWriter(runTimeFile, false);
      DateTime currentRun = DateTime.UtcNow;
      //file.WriteLine(currentRun.ToString());
      file.Close();
      List<string> IDs = new List<string>();
      int secondsSinceEpoch = (int)(lastRun - new DateTime(1970, 1, 1)).TotalSeconds;
      GetIDs(ref IDs, service, secondsSinceEpoch);
      Console.WriteLine(IDs.Count);

      List<string> SendersAccounts = GetEmails(IDs, service);

      Console.Read();

    }
  }
}
