﻿using System;
using Cognifide.PowerShell.Core.Settings;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Client.Commands
{
    [Serializable]
    public class EditIseSettings : ExecuteFieldEditor
    {
        protected const string AppNameParameter = "appName";
        protected const string PersonalParameter = "personal";

        protected bool Personalized { get; set; }
        protected string AppName { get; set; }

        public override CommandState QueryState(CommandContext context)
        {
            return CommandState.Enabled;
        }

        public override bool CanExecute(CommandContext context)
        {
            return true;
        }

        protected override void EnsureContext(ClientPipelineArgs args)
        {
            var settingsPath = ApplicationSettings.GetSettingsPath(AppName, Personalized);
            CurrentItem = Factory.GetDatabase(ApplicationSettings.SettingsDb).GetItem(settingsPath);

            Assert.IsNotNull(CurrentItem, CurrentItemIsNull);
            SettingsItem = Sitecore.Client.CoreDatabase.GetItem(args.Parameters[ButtonParameter]);
            Assert.IsNotNull(SettingsItem, SettingsItemIsNull);
        }

        public override void Execute(CommandContext context)
        {
            AppName = context.Parameters[AppNameParameter];
            Personalized = context.Parameters[PersonalParameter] == "1";

            Assert.ArgumentNotNull(context, "context");

            var settingsPath = ApplicationSettings.GetSettingsPath(AppName, Personalized);
            CurrentItem = Factory.GetDatabase(ApplicationSettings.SettingsDb).GetItem(settingsPath);
            if (CurrentItem == null)
            {
                var settings = ApplicationSettings.GetInstance(AppName, Personalized);
                settings.Save();
                CurrentItem = Factory.GetDatabase(ApplicationSettings.SettingsDb).GetItem(settingsPath);
            }

            Context.ClientPage.Start(this, "StartFieldEditor", new ClientPipelineArgs(context.Parameters)
            {
                Parameters =
                {
                    {"uri", CurrentItem.Uri.ToString()}
                }
            });
        }

        protected override void StartFieldEditor(ClientPipelineArgs args)
        {
            base.StartFieldEditor(args);
            ApplicationSettings.ReloadInstance(AppName, Personalized);

            if (!args.IsPostBack)
            {
                return;
            }

            var settings = ApplicationSettings.GetInstance(ApplicationNames.IseConsole);
            SheerResponse.Eval(String.Format("cognifide.powershell.changeFontSize({0});cognifide.powershell.changeFontFamily('{1}');", settings.FontSize, settings.FontFamily));
        }
    }
}