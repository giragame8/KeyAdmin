using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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

        // SUPPRESSION DE LA LISTE CODÉE EN DUR ! Tout est sur le Cloud maintenant.

        public FenetreLogin()
        {
            InitializeComponent();
            fichierMdp = Path.Combine(dossierAdmin, "admin.sec");
            if (!Directory.Exists(dossierAdmin)) Directory.CreateDirectory(dossierAdmin);
            VerifierIdentitePC();
        }

        private void VerifierIdentitePC()
        {
            if (File.Exists(fichierMdp))
            {
                try
                {
                    string[] parts = File.ReadAllText(fichierMdp).Split('|');
                    if (parts.Length == 2 && parts[0] == Environment.MachineName)
                    {
                        estPremier = false;
                        hashEnregistre = parts[1];
                    }
                }
                catch { }
                if (estPremier) File.Delete(fichierMdp);
            }

            TxtTitre.Text = estPremier ? "Nouveau PC : Créez votre accès" : "Connexion Sécurisée";
            BtnValider.Content = estPremier ? "Lier le PC et Créer" : "Déverrouiller";
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

            // On change l'apparence du bouton pendant qu'il interroge Google
            BtnValider.IsEnabled = false;
            string texteOriginal = BtnValider.Content.ToString();
            BtnValider.Content = "Vérification en cours...";

            // 1. On télécharge la liste des Admins depuis Google Sheets
            List<string> listeAdminsCloud = await CloudManager.GetEmailsAutorises();

            // Sécurité anti-blocage : Si tu n'as pas internet, on te laisse quand même entrer avec ton mail maître
            if (listeAdminsCloud.Count == 0)
            {
                listeAdminsCloud.Add("elliottmag8@gmail.com");
            }

            // 2. On vérifie si l'email tapé est dans le tableau Excel
            if (!listeAdminsCloud.Contains(email))
            {
                MessageBox.Show("Cet email n'est pas autorisé. Demandez au SuperAdmin de vous ajouter dans la base de données.", "Accès Refusé", MessageBoxButton.OK, MessageBoxImage.Error);
                BtnValider.IsEnabled = true;
                BtnValider.Content = texteOriginal;
                return;
            }

            // 3. Si l'email est bon, on vérifie le mot de passe local comme d'habitude
            string hash = HashMdp(mdp);
            if (estPremier)
            {
                File.WriteAllText(fichierMdp, $"{Environment.MachineName}|{hash}");
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

        private async System.Threading.Tasks.Task EnregistrerEtOuvrir(string email)
        {
            BtnValider.Content = "Connexion validée...";
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