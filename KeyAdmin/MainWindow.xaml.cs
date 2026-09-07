using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using SharedLogic;

namespace KeyAdmin
{
    public class LicenceGeneree
    {
        public string DateGen { get; set; }
        public string Entreprise { get; set; }
        public string NomClient { get; set; } = "En attente...";
        public string Role { get; set; }
        public string Cle { get; set; }
        public string Statut { get; set; }
        public bool PeutRevoquer => Statut == "Active";
        public string AffichageClient => $"{Entreprise} ({NomClient})";
    }

    public partial class MainWindow : Window
    {
        private readonly string CLE_SECRETE = "ElliottVideoIAPro_SecureKey_2026";
        public string AdminConnecte { get; set; }
        private string fichierHistorique = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KeyAdminElliott", "historique.csv");
        public ObservableCollection<LicenceGeneree> ListeLicences { get; set; }

        public MainWindow(string emailAdmin)
        {
            InitializeComponent();
            AdminConnecte = emailAdmin;
            this.Title = $"KeyAdmin - Connecté : {AdminConnecte}";
            DateExpiration.SelectedDate = DateTime.Now.AddYears(1);

            ListeLicences = new ObservableCollection<LicenceGeneree>();
            GridHistorique.ItemsSource = ListeLicences;

            ChargerEtSynchroniser();
        }

        private void BtnRafraichir_Click(object sender, RoutedEventArgs e)
        {
            ChargerEtSynchroniser();
        }

        private async void ChargerEtSynchroniser()
        {
            ListeLicences.Clear();

            if (File.Exists(fichierHistorique))
            {
                var lignes = File.ReadAllLines(fichierHistorique);
                foreach (var l in lignes)
                {
                    var p = l.Split(';');
                    if (p.Length == 5) ListeLicences.Add(new LicenceGeneree { DateGen = p[0], Entreprise = p[1], Role = p[2], Cle = p[3], Statut = p[4] });
                }
            }

            try
            {
                string data = await CloudManager.GetActivations();
                if (!string.IsNullOrEmpty(data))
                {
                    var mapping = new Dictionary<string, string>();
                    var paires = data.Split('|');

                    foreach (var paire in paires)
                    {
                        var elements = paire.Split(':');
                        if (elements.Length == 2)
                        {
                            mapping[elements[0]] = elements[1];
                        }
                    }

                    foreach (var lic in ListeLicences)
                    {
                        if (mapping.ContainsKey(lic.Cle)) lic.NomClient = mapping[lic.Cle];
                    }
                    GridHistorique.Items.Refresh();
                }
            }
            catch { }
        }

        private async void BtnGenerer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TxtHwid.Text) || string.IsNullOrEmpty(TxtEntreprise.Text))
            {
                MessageBox.Show("Veuillez remplir le HWID et l'Entreprise.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string uid = Guid.NewGuid().ToString().Substring(0, 8);
            string payload = $"{TxtHwid.Text.Trim()}|{TxtEntreprise.Text.Trim()}|{((ComboBoxItem)CboRole.SelectedItem).Content}|{DateExpiration.SelectedDate.Value:yyyy-MM-dd}|{uid}";

            string cle = ChiffrerAES(payload, CLE_SECRETE);

            var nouvelle = new LicenceGeneree
            {
                DateGen = DateTime.Now.ToString("dd/MM/yyyy"),
                Entreprise = TxtEntreprise.Text.Trim(),
                Role = ((ComboBoxItem)CboRole.SelectedItem).Content.ToString(),
                Cle = cle,
                Statut = "Active"
            };

            ListeLicences.Insert(0, nouvelle);
            SauvegarderLocal();

            await CloudManager.EnvoyerAction("GEN", $"{AdminConnecte}|{nouvelle.Entreprise}|{nouvelle.Role}|{cle}|{DateExpiration.SelectedDate.Value:yyyy-MM-dd}");
            TxtCleResultat.Text = cle;
        }

        private async void BtnRevoquer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is LicenceGeneree licence)
            {
                if (MessageBox.Show($"Révoquer {licence.Entreprise} ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    licence.Statut = "Révoquée";
                    SauvegarderLocal();
                    GridHistorique.Items.Refresh();

                    await CloudManager.EnvoyerAction("REV", $"{AdminConnecte}|{licence.Cle}");
                    MessageBox.Show("Ordre de révocation envoyé au serveur ! L'accès du client sera coupé au prochain démarrage.");
                }
            }
        }

        private void SauvegarderLocal()
        {
            var sb = new StringBuilder();
            foreach (var l in ListeLicences) sb.AppendLine($"{l.DateGen};{l.Entreprise};{l.Role};{l.Cle};{l.Statut}");
            File.WriteAllText(fichierHistorique, sb.ToString());
        }

        private string ChiffrerAES(string texte, string mdp)
        {
            byte[] iv = new byte[16];
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(mdp);
                aes.IV = iv;
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(texte);
                        }
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
    }
}