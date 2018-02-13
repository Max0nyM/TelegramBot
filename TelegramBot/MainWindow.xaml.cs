using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SimpleJSON;
using xNet;
using ChatSharp;

namespace TelegramBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

       

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Thread StartLoop = new Thread(BotLoop);
            StartLoop.IsBackground = true;
            StartLoop.Start();
        }


        void BotLoop()
        {

            var client = new IrcClient("irc.freenode.net", new IrcUser("ChatSharp", "ChatSharp"+new Random().Next(0,1000)));

            client.ConnectionComplete += (s, e) =>
            {
                Console.Write("test");
                client.JoinChannel("##an0nymC");
                richTextBox.Dispatcher.Invoke(
                               new UpdateTextCallback(this.UpdateText),
                               new object[] { client.Users.Count().ToString(), "good" }
                           );
          
            };
        
            client.ChannelMessageRecieved += (s, e) =>
            {
                var channel = client.Channels[e.PrivateMessage.Source];
                Console.Write(channel.Users.Count());
                if (e.PrivateMessage.Message == ".list")
                    channel.SendMessage(string.Join(", ", channel.Users.Select(u => u.Nick)));
                else if (e.PrivateMessage.Message.StartsWith(".ban "))
                {
                    if (!channel.UsersByMode['@'].Contains(client.User))
                    {
                        channel.SendMessage("I'm not an op here!");
                        return;
                    }
                    var target = e.PrivateMessage.Message.Substring(5);
                    client.WhoIs(target, whois => channel.ChangeMode("+b *!*@" + whois.User.Hostname));
                }
            };

            client.ConnectAsync();


            while (true)
            {
                GetUpdates();
                Thread.Sleep(1000);
            }
        }

        public delegate void UpdateTextCallback(string message, string result);

        private void UpdateText(string message, string result)
        {
          
                richTextBox.AppendText(message + Environment.NewLine);
                richTextBox.ScrollToEnd();
            
      
        }
        void GetUpdates()
        {
            using (var http = new HttpRequest())
            {
                string response = http.Get(Constants.ApiUrl+Constants.Token+"/getUpdates" + "?offset=" + (Constants.LastUpdateId + 1)).ToString();
                var N = JSON.Parse(response);
                foreach (JSONNode res in N["result"].AsArray)
                {
                    Constants.LastUpdateId = res["update_id"].AsInt;
                    richTextBox.Dispatcher.Invoke(
                                new UpdateTextCallback(this.UpdateText),
                                new object[] { res["message"]["text"].ToString(), "good" }
                            );

                    SendMessage("Я получил твоё сообщение", res["message"]["chat"]["id"].AsInt);
                }
            }
        }

        static void SendMessage(string message, int chatid)
        {
            using (var http = new HttpRequest())
            {
             
                RequestParams pParams = new RequestParams();
                pParams["text"] = message;
                pParams["chat_id"] = chatid.ToString();
                http.Post(Constants.ApiUrl + Constants.Token+ "/sendMessage", pParams);

            }
        }
    }
}
