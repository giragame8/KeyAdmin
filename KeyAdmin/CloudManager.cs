using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SharedLogic
{
    public static class CloudManager
    {
        public static readonly string MasterAppUrl = "https://script.google.com/macros/s/AKfycbxpYvDVOppiqGLMfzan6bIUIyNqBa_XMv79EMrGnxSrqmSCpFPkkBAkXEs_GEttLh4L/exec";

        public static string UserAppUrl = "";
        public static string CleSecreteUtilisateur = "";

        private static string UrlEffective => string.IsNullOrWhiteSpace(UserAppUrl) ? MasterAppUrl : UserAppUrl;

        private static HttpClient ObtenirClientHttp()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true
            };
            return new HttpClient(handler);
        }

        public static async Task<List<string>> GetClientsKeyAdminAutorises()
        {
            try
            {
                using (HttpClient client = ObtenirClientHttp())
                {
                    string response = await client.GetStringAsync(MasterAppUrl + "?type=clients_keyadmin");
                    if (string.IsNullOrWhiteSpace(response)) return new List<string>();
                    return new List<string>(response.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                }
            }
            catch { return new List<string>(); }
        }

        public static async Task<string> VerifierMiseAJourAdmin()
        {
            try
            {
                using (HttpClient client = ObtenirClientHttp())
                {
                    return await client.GetStringAsync(MasterAppUrl + "?type=update_admin");
                }
            }
            catch { return ""; }
        }

        public static async Task EnvoyerAction(string action, string donnees)
        {
            try
            {
                using (HttpClient client = ObtenirClientHttp())
                {
                    var parametres = new Dictionary<string, string>
                    {
                        { "action", action },
                        { "data", donnees }
                    };
                    await client.PostAsync(UrlEffective, new FormUrlEncodedContent(parametres));
                }
            }
            catch { }
        }

        public static async Task<string> GetActivations()
        {
            try
            {
                using (HttpClient client = ObtenirClientHttp()) { return await client.GetStringAsync(UrlEffective + "?type=activations"); }
            }
            catch { return ""; }
        }

        public static async Task<List<string>> GetEmailsAutorises()
        {
            try
            {
                using (HttpClient client = ObtenirClientHttp())
                {
                    string response = await client.GetStringAsync(UrlEffective + "?type=admins");
                    if (string.IsNullOrWhiteSpace(response)) return new List<string>();
                    return new List<string>(response.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                }
            }
            catch { return new List<string>(); }
        }

        public static async Task<bool> EstCleRevoguee(string cle)
        {
            try
            {
                using (HttpClient client = ObtenirClientHttp())
                {
                    string blacklist = await client.GetStringAsync(UrlEffective);
                    return blacklist.Contains(cle);
                }
            }
            catch { return false; }
        }
    }
}