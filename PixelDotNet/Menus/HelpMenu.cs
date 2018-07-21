/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

//#define CRASH

using PixelDotNet.Actions;
using PixelDotNet.SystemLayer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PixelDotNet.Menus
{
    internal sealed class HelpMenu
        : PdnMenuItem
    {
        private PdnMenuItem menuHelpHelpTopics;
        private ToolStripSeparator menuHelpSeparator1;
        private PdnMenuItem menuHelpPdnWebsite;
        private ToolStripSeparator menuHelpSeparator2;
        private PdnMenuItem menuHelpLanguage;
        private PdnMenuItem menuHelpLanguageSentinel;
        private ToolStripSeparator menuHelpSeparator3;
#if CRASH
        private PdnMenuItem menuHelpCrash;
#endif
        private PdnMenuItem menuHelpAbout;

        public HelpMenu()
        {
#if CRASH
            if (PdnInfo.IsFinalBuild)
            {
                throw new Exception("Do not leave CRASH defined for an actual release build!");
            }
#endif

            InitializeComponent();
        }

        protected override void OnAppWorkspaceChanged()
        {
            base.OnAppWorkspaceChanged();
        }

        private void InitializeComponent()
        {
            this.menuHelpHelpTopics = new PdnMenuItem();
            this.menuHelpSeparator1 = new ToolStripSeparator();
            this.menuHelpPdnWebsite = new PdnMenuItem();
            this.menuHelpSeparator2 = new ToolStripSeparator();
            this.menuHelpLanguage = new PdnMenuItem();
            this.menuHelpLanguageSentinel = new PdnMenuItem();
            this.menuHelpSeparator3 = new ToolStripSeparator();
#if CRASH
            this.menuHelpCrash = new PdnMenuItem();
#endif
            this.menuHelpAbout = new PdnMenuItem();
            //
            // HelpMenu
            //
            this.DropDownItems.AddRange(
                new ToolStripItem[]
                {
                    this.menuHelpHelpTopics,
                    this.menuHelpSeparator1,
                    this.menuHelpPdnWebsite,
                    this.menuHelpSeparator2,
                    this.menuHelpLanguage,
                    this.menuHelpSeparator3,
#if CRASH
                    this.menuHelpCrash,
#endif
                    this.menuHelpAbout
                });
            this.Name = "Menu.Help";
            this.Text = PdnResources.GetString("Menu.Help.Text");
            // 
            // menuHelpHelpTopics
            // 
            this.menuHelpHelpTopics.Name = "HelpTopics";
            this.menuHelpHelpTopics.ShortcutKeys = Keys.F1;
            this.menuHelpHelpTopics.Click += new System.EventHandler(this.MenuHelpHelpTopics_Click);
            //
            // menuHelpPdnWebsite
            //
            this.menuHelpPdnWebsite.Name = "PdnWebsite";
            this.menuHelpPdnWebsite.Click += new EventHandler(MenuHelpPdnWebsite_Click);
            //
            // menuHelpLanguage
            //
            this.menuHelpLanguage.Name = "Language";
            this.menuHelpLanguage.DropDownItems.AddRange(
                new ToolStripItem[] 
                {
                    this.menuHelpLanguageSentinel
                });
            this.menuHelpLanguage.DropDownOpening += new EventHandler(MenuHelpLanguage_DropDownOpening);
            // 
            // menuHelpLanguageSentinel
            //
            this.menuHelpLanguageSentinel.Text = "(sentinel)";
            //

#if CRASH
            //
            // menuHelpCrash
            this.menuHelpCrash.Name = "Crash";
            this.menuHelpCrash.Text = "Crash!";
            this.menuHelpCrash.ShortcutKeys = Keys.Control | Keys.Alt | Keys.X;
            this.menuHelpCrash.Click += delegate { ((Control)null).Dispose(); };
#endif

            // 
            // menuHelpAbout
            // 
            this.menuHelpAbout.Name = "About";
            this.menuHelpAbout.Click += new System.EventHandler(this.MenuHelpAbout_Click);
        }
        
        private void MenuHelpPdnWebsite_Click(object sender, EventArgs e)
        {
            PdnInfo.LaunchWebSite(AppWorkspace, InvariantStrings.WebsitePageHelpMenu);
        }

        private void MenuHelpAbout_Click(object sender, System.EventArgs e)
        {
            using (AboutDialog af = new AboutDialog())
            {
                af.ShowDialog(AppWorkspace);
            }
        }

        private void MenuHelpHelpTopics_Click(object sender, System.EventArgs e)
        {
            Utility.ShowHelp(AppWorkspace);
        }

        private class MenuTitleAndLocale
        {
            public string title;
            public string locale;

            public MenuTitleAndLocale(string title, string locale)
            {
                this.title = title;
                this.locale = locale;
            }
        }

        private string GetCultureInfoName(CultureInfo ci)
        {
            CultureInfo en_US = new CultureInfo("en-US");

            // For "English (United States)" we'd rather just display "English"
            if (ci.Equals(en_US))
            {
                return GetCultureInfoName(ci.Parent);
            }
            else
            {
                return ci.NativeName;
            }
        }

        private void MenuHelpLanguage_DropDownOpening(object sender, EventArgs e)
        {
            this.menuHelpLanguage.DropDownItems.Clear();

            string[] locales = PdnResources.GetInstalledLocales();

            MenuTitleAndLocale[] mtals = new MenuTitleAndLocale[locales.Length];

            for (int i = 0; i < locales.Length; ++i)
            {
                string locale = locales[i];
                CultureInfo ci = new CultureInfo(locale);
                mtals[i] = new MenuTitleAndLocale(ci.DisplayName, locale);
            }

            Array.Sort(
                mtals,
                delegate(MenuTitleAndLocale x, MenuTitleAndLocale y)
                {
                    return string.Compare(x.title, y.title, StringComparison.InvariantCultureIgnoreCase);
                });

            foreach (MenuTitleAndLocale mtal in mtals)
            {
                ToolStripMenuItem menuItem = new ToolStripMenuItem();
                menuItem.Text = GetCultureInfoName(new CultureInfo(mtal.locale));
                menuItem.Tag = mtal.locale;
                menuItem.Click += new EventHandler(LanguageMenuItem_Click);

                if (0 == string.Compare(mtal.locale, CultureInfo.CurrentUICulture.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    menuItem.Checked = true;
                }

                this.menuHelpLanguage.DropDownItems.Add(menuItem);
            }
        }

        private void LanguageMenuItem_Click(object sender, EventArgs e)
        {
            // Save off the old locale name in case they decide to cancel
            string oldLocaleName = PdnResources.Culture.Name;

            // Now, apply the chosen language so that the confirmation buttons show up in the right language
            ToolStripMenuItem miwt = (ToolStripMenuItem)sender;
            string newLocaleName = (string)miwt.Tag;
            PdnResources.SetNewCulture(newLocaleName);

            // Load the text and buttons in the new language
            Icon formIcon = Utility.ImageToIcon(PdnResources.GetImageResource("Icons.MenuHelpLanguageIcon.png").Reference);
            string title = PdnResources.GetString("ConfirmLanguageDialog.Title");
            Image taskImage = null;
            string introText = PdnResources.GetString("ConfirmLanguageDialog.IntroText");

            Image restartImage = PdnResources.GetImageResource("Icons.RightArrowBlue.png").Reference;
            string explanationTextFormat = PdnResources.GetString("ConfirmLanguageDialog.RestartTB.ExplanationText.Format");
            CultureInfo newCI = new CultureInfo(newLocaleName);

            // We prefer to show "English (United States)" as just "English"
            CultureInfo en_US = new CultureInfo("en-US");

            if (newCI.Equals(en_US))
            {
                newCI = newCI.Parent;
            }

            string languageName = newCI.NativeName;
            string explanationText = string.Format(explanationTextFormat, languageName);

            TaskButton restartTB = new TaskButton(
                restartImage,
                PdnResources.GetString("ConfirmLanguageDialog.RestartTB.ActionText"),
                explanationText);

            Image cancelImage = PdnResources.GetImageResource("Icons.CancelIcon.png").Reference;
            TaskButton cancelTB = new TaskButton(
                cancelImage,
                PdnResources.GetString("ConfirmLanguageDialog.CancelTB.ActionText"),
                PdnResources.GetString("ConfirmLanguageDialog.CancelTB.ExplanationText"));

            int width96dpi = (TaskDialog.DefaultPixelWidth96Dpi * 5) / 4;

            TaskButton clickedTB = TaskDialog.Show(
                AppWorkspace,
                formIcon,
                title,
                taskImage,
                true,
                introText,
                new TaskButton[] { restartTB, cancelTB },
                restartTB,
                cancelTB,
                width96dpi);

            if (clickedTB == restartTB)
            {
                // Next, apply restart logic
                CloseAllWorkspacesAction cawa = new CloseAllWorkspacesAction();
                cawa.PerformAction(AppWorkspace);

                if (!cawa.Cancelled)
                {
                    SystemLayer.Shell.RestartApplication();
                    Startup.CloseApplication();
                }
            }
            else
            {
                // Revert to the old language
                PdnResources.SetNewCulture(oldLocaleName);
            }
        }
    }
}
