using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SharedLogic;

namespace KeyAdmin
{
    public partial class FenetreLogin : Window
    {
        private string dossierAdmin = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KeyAdminElliott");
        private string fichierMdp;
        private bool estPremier = true;
        private string hashEnregistre = "";

        public FenetreLogin()
        {
            InitializeComponent();
            fichierMdp = Path.Combine(dossierAdmin, "admin.sec");
            if (!Directory.Exists(dossierAdmin)) Directory.CreateDirectory(dossierAdmin);

            VerifierIdentitePC();
            this.Loaded += FenetreLogin_Loaded;
        }

        private async void FenetreLogin_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string updateData = await CloudManager.VerifierMiseAJourAdmin();
                if (!string.IsNullOrEmpty(updateData) && updateData.Contains("|"))
                {
                    string[] parts = updateData.Split('|');
                    Version versionActuelle = Assembly.GetExecutingAssembly().GetName().Version;

                    if (Version.TryParse(parts[0].Trim(), out Version versionEnLigne) && versionEnLigne > versionActuelle)
                    {
                        if (MessageBox.Show($"Une nouvelle version de KeyAdmin est disponible ({versionEnLigne}). Télécharger ?", "Mise à jour", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            Process.Start(new ProcessStartInfo(parts[1].Trim()) { UseShellExecute = true });
                            Application.Current.Shutdown();
                        }
                    }
                }
            }
            catch { }
        }

        private void VerifierIdentitePC()
        {
            if (File.Exists(fichierMdp))
            {
                try
                {
                    string[] parts = File.ReadAllText(fichierMdp).Split('|');
                    // Format attendu: MachineName | Hash | ScriptUrl(Base64) | CleSecrete(Base64)
                    if (parts.Length >= 4 && parts[0] == Environment.MachineName)
                    {
                        estPremier = false;
                        hashEnregistre = parts[1];

                        // Chargement des données du client dans le CloudManager
                        CloudManager.UserAppUrl = Encoding.UTF8.GetString(Convert.FromBase64String(parts[2]));
                        CloudManager.CleSecreteUtilisateur = Encoding.UTF8.GetString(Convert.FromBase64String(parts[3]));
                    }
                }
                catch { estPremier = true; }
                if (estPremier) File.Delete(fichierMdp);
            }

            if (estPremier)
            {
                TxtTitre.Text = "Nouveau PC : Configuration";
                BtnValider.Content = "Lier le PC et Configurer";
                PanelConfiguration.Visibility = Visibility.Visible;
                this.Height = 580; // Agrandit la fenêtre pour laisser la place aux nouveaux champs
            }
        }

        private async void BtnValider_Click(object sender, RoutedEventArgs e)
        {
            string email = TxtEmail.Text.Trim().ToLower();
            string mdp = TxtMotDePasse.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(mdp))
            {
                MessageBox.Show("Veuillez remplir l'email et le mot de passe.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnValider.IsEnabled = false;
            string texteOriginal = BtnValider.Content.ToString();
            BtnValider.Content = "Vérification...";

            // 1. On vérifie ton fichier Master pour voir s'il a le droit d'utiliser KeyAdmin
            List<string> clientsAutorises = await CloudManager.GetClientsKeyAdminAutorises();
            if (clientsAutorises.Count == 0) clientsAutorises.Add("elliottmag8@gmail.com");

            if (!clientsAutorises.Contains(email))
            {
                MessageBox.Show("Votre accès à KeyAdmin n'est pas autorisé. Contactez l'éditeur.", "Accès Refusé", MessageBoxButton.OK, MessageBoxImage.Error);
                BtnValider.IsEnabled = true;
                BtnValider.Content = texteOriginal;
                return;
            }

            // 2. Traitement de la première configuration ou de la connexion normale
            string hash = HashMdp(mdp);
            if (estPremier)
            {
                string scriptUrl = TxtScriptUrl.Text.Trim();
                string cleSecrete = TxtCleSecrete.Text.Trim();

                if (string.IsNullOrWhiteSpace(scriptUrl) || string.IsNullOrWhiteSpace(cleSecrete))
                {
                    MessageBox.Show("Veuillez renseigner votre URL Google Script et votre Clé Secrète.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    BtnValider.IsEnabled = true;
                    BtnValider.Content = texteOriginal;
                    return;
                }

                CloudManager.UserAppUrl = scriptUrl;
                CloudManager.CleSecreteUtilisateur = cleSecrete;

                // Sauvegarde sécurisée (encodage basique pour éviter que ça traîne en texte clair)
                string b64Url = Convert.ToBase64String(Encoding.UTF8.GetBytes(scriptUrl));
                string b64Cle = Convert.ToBase64String(Encoding.UTF8.GetBytes(cleSecrete));
                File.WriteAllText(fichierMdp, $"{Environment.MachineName}|{hash}|{b64Url}|{b64Cle}");

                await EnregistrerEtOuvrir(email);
            }
            else
            {
                if (hash == hashEnregistre)
                {
                    await EnregistrerEtOuvrir(email);
                }
                else
                {
                    MessageBox.Show("Mot de passe local incorrect.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    BtnValider.IsEnabled = true;
                    BtnValider.Content = texteOriginal;
                }
            }
        }

        private async Task EnregistrerEtOuvrir(string email)
        {
            BtnValider.Content = "Connexion validée...";
            // L'historique des connexions est envoyé sur LEUR script, pas le tien
            await CloudManager.EnvoyerAction("LOG", $"{email}|{Environment.MachineName}");

            MainWindow main = new MainWindow(email);
            main.Show();
            this.Close();
        }

        private string HashMdp(string m)
        {
            using (var s = SHA256.Create())
            {
                byte[] b = s.ComputeHash(Encoding.UTF8.GetBytes(m));
                StringBuilder sb = new StringBuilder();
                foreach (byte x in b) sb.Append(x.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}