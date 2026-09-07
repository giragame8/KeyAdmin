using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SharedLogic
{
    public static class CloudManager
    {
        // Ton script à toi (Master) pour vérifier qui peut ouvrir KeyAdmin
        public static readonly string MasterAppUrl = "https://script.google.com/macros/s/AKfycbxpYvDVOppiqGLMfzan6bIUIyNqBa_XMv79EMrGnxSrqmSCpFPkkBAkXEs_GEttLh4L/exec";

        // Variables dynamiques définies lors de la connexion du client
        public static string UserAppUrl = "";
        public static string CleSecreteUtilisateur = "";

        // --- FONCTIONS MASTER (Vérification KeyAdmin) ---
        public static async Task<List<string>> GetClientsKeyAdminAutorises()
        {
            try
            {
                using (HttpClient client = new HttpClient())
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
                using (HttpClient client = new HttpClient())
                {
                    return await client.GetStringAsync(MasterAppUrl + "?type=update_admin");
                }
            }
            catch { return ""; }
        }

        // --- FONCTIONS UTILISATEUR (Gestion de LEUR logiciel) ---
        public static async Task EnvoyerAction(string action, string donnees)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var parametres = new Dictionary<string, string> { { "action", action }, { "data", donnees } };
                    await client.PostAsync(UserAppUrl, new FormUrlEncodedContent(parametres));
                }
            }
            catch { }
        }

        public static async Task<string> GetActivations()
        {
            try
            {
                using (HttpClient client = new HttpClient()) { return await client.GetStringAsync(UserAppUrl + "?type=activations"); }
            }
            catch { return ""; }
        }

        public static async Task<List<string>> GetEmailsAutorises()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string response = await client.GetStringAsync(UserAppUrl + "?type=admins");
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
                using (HttpClient client = new HttpClient())
                {
                    string blacklist = await client.GetStringAsync(UserAppUrl);
                    return blacklist.Contains(cle);
                }
            }
            catch { return false; }
        }
    }
}