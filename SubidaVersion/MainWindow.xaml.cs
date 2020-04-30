using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Windows;
using Nancy.Json;
using RestSharp;
using RestSharp.Authenticators;

namespace SubidaVersion
{
    public partial class MainWindow : Window
    {
        private string request = "/rest/api/3/search?jql=";
        private string jiraUsername = string.Empty;
        private string jiraPassword = string.Empty;
        private bool rememberJira = false;
        private string bitbucketUsername = string.Empty;
        private string bitbucketPassword = string.Empty;
        private bool rememberBitbucket = false;
        private string branch = "master";
        private string keyWord = string.Empty;

        public MainWindow()
        {
            InitializeComponent();

            //cargamos valores si los tiene guardados
            if (!String.IsNullOrEmpty(Properties.Settings.Default.JiraUserName) && !String.IsNullOrEmpty(Properties.Settings.Default.JiraPassword))
            {
                JiraUsername.Text = Properties.Settings.Default.JiraUserName;
                JiraPassword.Password = Properties.Settings.Default.JiraPassword;
                RememberJira.IsChecked = true;
            }

            if (!String.IsNullOrEmpty(Properties.Settings.Default.BitbucketUserName) && !String.IsNullOrEmpty(Properties.Settings.Default.BitbucketPassword))
            {
                BitbucketUsername.Text = Properties.Settings.Default.BitbucketUserName;
                BitbucketPassword.Password = Properties.Settings.Default.BitbucketPassword;
                RememberBitbucket.IsChecked = true;
            }
        }

        public void RunSubidaVersion(object sender, RoutedEventArgs e)
        {
            jiraUsername = JiraUsername.Text;
            jiraPassword = JiraPassword.Password;
            rememberJira = RememberJira.IsChecked.HasValue ? RememberJira.IsChecked.Value : false;

            var Tasks = GetTasks(jiraUsername, jiraPassword, Project.Text, FixVersion.Text, request, rememberJira);

            var TasksExceptions = new List<string>();

            if (!String.IsNullOrEmpty(Exceptions.Text.Trim()))
            {
                var exceptions = Exceptions.Text.Split(',');
                foreach (var ex in exceptions.ToList())
                {
                    TasksExceptions.Add(ex.Trim());
                }
            }
            
            foreach (var TaskExc in TasksExceptions)
            {
                Tasks.issues.RemoveAll(x => x.key == TaskExc);
            }

            if (!String.IsNullOrEmpty(Branch.Text))
                branch = Branch.Text;

            bitbucketUsername = BitbucketUsername.Text;
            bitbucketPassword = BitbucketPassword.Password;
            rememberBitbucket = RememberBitbucket.IsChecked.HasValue ? RememberJira.IsChecked.Value : false;
            keyWord = KeyWord.Text;

            var Cherry_Picks = GetCherryPicks(bitbucketUsername, bitbucketPassword, Repo.Text, branch, Tasks, rememberBitbucket, keyWord);
            Cherry_Picks.Reverse(); //invertimos el orden para tenerlos de mas viejos a mas nuevos

            foreach (var CherryPick in Cherry_Picks)
            {
                CherryPicks.Text += CherryPick + '\n';
            }
        }

        public void CloseWindows(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private static TaskRS GetTasks(string JiraUsername, string JiraPassword, string JiraProject, string JiraFixVersion, string Request, bool RememberJira)
        {
            var jiraClient = new RestClient("APIJiraUrl");
            //utilizamos la propiedad urlencode por si el projecto o la fix version tiene espacios
            Request += "project%3D" + HttpUtility.UrlEncode(JiraProject) + "%20and%20fixVersion%3D%27" + HttpUtility.UrlEncode(JiraFixVersion) + "%27";
            RestRequest jiraRequest = new RestRequest(Request);
            jiraClient.Authenticator = new HttpBasicAuthenticator(JiraUsername, JiraPassword);
            var jiraResponse = jiraClient.Execute(jiraRequest);

            //comprobamos si quiere guardar el login
            if (RememberJira)
            {
                Properties.Settings.Default.JiraUserName = JiraUsername;
                Properties.Settings.Default.JiraPassword = JiraPassword;
                Properties.Settings.Default.Save();
            }
            else
            {
                //si deshabilita el check recordar y tiene valores, borrarlos
                if (!String.IsNullOrEmpty(Properties.Settings.Default.JiraUserName) && !String.IsNullOrEmpty(Properties.Settings.Default.JiraPassword))
                {
                    Properties.Settings.Default.JiraUserName = string.Empty;
                    Properties.Settings.Default.JiraPassword = string.Empty;
                    Properties.Settings.Default.Save();
                }
            }

            return new JavaScriptSerializer().Deserialize<TaskRS>(jiraResponse.Content);
        }

        private static List<string> GetCherryPicks(string BitbucketUsername, string BitbucketPassword, string Repo, string Branch, TaskRS Tareas, bool RememberBitbucket, string KeyWord)
        {
            var bitbucketClient = new RestClient("https://api.bitbucket.org/2.0/");
            bitbucketClient.Authenticator = new HttpBasicAuthenticator(BitbucketUsername, BitbucketPassword);
            //utilizamos la propiedad urlencode por si el repo o el branch tiene espacios
            var bitbucketRequest = new RestRequest(string.Format("repositories/{WorkSapce}/{0}/commits/{1}", HttpUtility.UrlEncode(Repo), HttpUtility.UrlEncode(Branch)));
            var bitbucketResponse = bitbucketClient.Execute(bitbucketRequest);
            var json = bitbucketResponse.Content;
            var Commits = new JavaScriptSerializer().Deserialize<CommitRS>(json);
            var ListCherryPicks = new List<string>();

            //comprobamos si quiere guardar el login
            if (RememberBitbucket)
            {
                Properties.Settings.Default.BitbucketUserName = BitbucketUsername;
                Properties.Settings.Default.BitbucketPassword = BitbucketPassword;
                Properties.Settings.Default.Save();
            }
            else
            {
                //si deshabilita el check recordar y tiene valores, borrarlos
                if (!String.IsNullOrEmpty(Properties.Settings.Default.BitbucketUserName) && !String.IsNullOrEmpty(Properties.Settings.Default.BitbucketPassword))
                {
                    Properties.Settings.Default.BitbucketUserName = string.Empty;
                    Properties.Settings.Default.BitbucketPassword = string.Empty;
                    Properties.Settings.Default.Save();
                }
            }

            //customfield_12500.Contains("{}") quiere decir que la tarea no tiene commits por lo tanto las quitamos
            Tareas.issues.RemoveAll(x => x.fields.customfield_12500.Contains("{}"));

            //control bucle infinito
            int pages = 1;
            while (Tareas.issues.Count != ListCherryPicks.Count)
            {
                Commits.values.RemoveAll(x => x.message.Contains("Revert") || !x.message.Contains(KeyWord));

                foreach (var Tarea in Tareas.issues)
                {
                    if (Commits.values.OrderBy(x => x.date).Any(x => x.message.Contains(Tarea.key)) && !ListCherryPicks.Any(y => y.StartsWith(Tarea.key)))
                        ListCherryPicks.Add(Tarea.key + " - git cherry-pick -n " + Commits.values.OrderBy(x => x.date).First(x => x.message.Contains(Tarea.key)).hash);
                }

                if (Tareas.issues.Count > ListCherryPicks.Count)
                {
                    bitbucketRequest = new RestRequest(Commits.next);
                    bitbucketResponse = bitbucketClient.Execute(bitbucketRequest);
                    json = bitbucketResponse.Content;
                    Commits = new JavaScriptSerializer().Deserialize<CommitRS>(json);
                }

                //aun con los controles de tareas sin commits y el campo para omitira tareas solo miramos las 10 primeras paginas para evitar un bucle infinito
                if (pages > 10)
                    break;

                pages++;
            }

            return ListCherryPicks;
        }

        private static void SaveLogin()
        {

        }
    }
}
