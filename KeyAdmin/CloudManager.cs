using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SharedLogic
{
    public static class CloudManager
    {
        private static readonly string WebAppUrl = "https://script.google.com/macros/s/AKfycbwgYyabBkwO81pYBauT82ALVLJ0hJdcy4iu1K0z6Skjqwf1_C2CIckf1GJhnCzm7yOm/exec";

        public static async Task EnvoyerAction(string action, string donnees)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var parametres = new Dictionary<string, string> { { "action", action }, { "data", donnees } };
                    await client.PostAsync(WebAppUrl, new FormUrlEncodedContent(parametres));
                }
            }
            catch { }
        }

        // Récupère la liste des activations (Clé -> Nom)
        public static async Task<string> GetActivations()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    return await client.GetStringAsync(WebAppUrl + "?type=activations");
                }
            }
            catch { return ""; }
        }
        // NOUVEAU : Récupère la liste des emails depuis l'onglet "Admins"
        public static async Task<List<string>> GetEmailsAutorises()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string response = await client.GetStringAsync(WebAppUrl + "?type=admins");
                    if (string.IsNullOrWhiteSpace(response)) return new List<string>();

                    // On découpe la réponse (séparée par des virgules) en liste
                    return new List<string>(response.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                }
            }
            catch
            {
                return new List<string>(); // Retourne vide si pas d'internet
            }
        }
        public static async Task<bool> EstCleRevoguee(string cle)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string blacklist = await client.GetStringAsync(WebAppUrl);
                    return blacklist.Contains(cle);
                }
            }
            catch { return false; }
        }
    }
}