using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Apollo
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            bool FirstRun = CheckIfFirstRun();
            if (FirstRun)
            {
                BetaWindow betaWindow = new BetaWindow();
                betaWindow.ShowDialog();

                SetFirstRunFlag();
            }
        }

        private bool CheckIfFirstRun()
        {
            string FirstRunValue = ConfigurationManager.AppSettings["IsFirstRun"];
            return string.IsNullOrEmpty(FirstRunValue) || FirstRunValue == "true";
        }

        private void SetFirstRunFlag()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings.Remove("isFirstRun");
            config.AppSettings.Settings.Add("isFirstRun", "false");
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
