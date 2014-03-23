using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;


namespace WinKeyToo
{
    [RunInstaller(true)]
    public partial class WinKeyTooInstaller : Installer
    {
        public WinKeyTooInstaller()
        {
            InitializeComponent();
        }

        private RegistryKey RunKey
        {
            get
            {
                // ReSharper disable PossibleNullReferenceException
                return Registry.CurrentUser.OpenSubKey("Software")
                    .OpenSubKey("Microsoft")
                    .OpenSubKey("Windows")
                    .OpenSubKey("CurrentVersion")
                    .OpenSubKey("Run", true);
                // ReSharper restore PossibleNullReferenceException
            }
        }

        public override void Commit(IDictionary savedState)
        {
            base.Commit(savedState);
            var runKey = RunKey;
            if (runKey != null)
            {
                runKey.SetValue("WinKeyToo", Context.Parameters["assemblypath"]);
            }

            Process.Start(Context.Parameters["assemblypath"], "1TI");
        }

        public override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);
            var runKey = RunKey;
            if (runKey != null && !string.IsNullOrEmpty(runKey.GetValue("WinKeyToo", string.Empty).ToString()))
            {
                runKey.DeleteValue("WinKeyToo", false);
            }
        }
    }
}
