﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Core.Settings
{
    public class ApplicationSettings
    {
        public const string SettingsItemPath = "/sitecore/system/Modules/PowerShell/Settings/";
        public const string IseSettingsItemAllUsers = "All Users";
        public const string FolderTemplatePath = "/sitecore/templates/Common/Folder";
        public const string ScriptLibraryPath = "/sitecore/system/Modules/PowerShell/Script Library/";

        private static string rulesDb = null;
        private static string settingsDb = null;
        private static string scriptLibraryDb = null;

        public static string RulesDb {
            get
            {
                GetDatabaseName(ref rulesDb, "powershell/workingDatabase/rules");
                return rulesDb;
            }
        }

        public static string SettingsDb
        {
            get
            {
                GetDatabaseName(ref settingsDb, "powershell/workingDatabase/settings");
                return settingsDb;
            }
        }

        public static string ScriptLibraryDb
        {
            get
            {
                GetDatabaseName(ref scriptLibraryDb, "powershell/workingDatabase/scriptLibrary");
                return scriptLibraryDb;
            }
        }

        private static void GetDatabaseName(ref string databaseName, string settingPath)
        {
            if (String.IsNullOrEmpty(databaseName))
            {
                databaseName = Factory.GetString(settingPath, false);
                if (String.IsNullOrEmpty(databaseName))
                {
                    databaseName = "master";
                }
            }
        }


        private static readonly Dictionary<string, ApplicationSettings> instances =
            new Dictionary<string, ApplicationSettings>();

        private ApplicationSettings(string applicationName)
        {
            ApplicationName = applicationName;
        }

        protected bool Loaded { get; private set; }

        public string Prescript { get; set; }
        public string LastScript { get; set; }
        public bool SaveLastScript { get; set; }
        public bool UseTypeInfo { get; set; }
        public int HostWidth { get; set; }
        public ConsoleColor ForegroundColor { get; set; }
        public ConsoleColor BackgroundColor { get; set; }
        public string ApplicationName { get; private set; }
        public int FontSize { get; set; }
        public string FontFamily { get; set; }

        public static string GetSettingsPath(string applicationName, bool personalizedSettings)
        {
            return SettingsItemPath + GetSettingsName(applicationName, personalizedSettings);
        }

        public static string GetSettingsName(string applicationName, bool personalizedSettings)
        {
            return applicationName +
                   (personalizedSettings
                       ? "/" + CurrentDomain + "/" + CurrentUserName
                       : "/All Users");
        }

        public string AppSettingsPath
        {
            get { return SettingsItemPath + ApplicationName + "/"; }
        }

        public string CurrentUserSettingsPath
        {
            get { return AppSettingsPath + CurrentDomain + "/" + CurrentUserName; }
        }

        public string AllUsersSettingsPath
        {
            get { return AppSettingsPath + IseSettingsItemAllUsers; }
        }

        private static readonly char[] invalidChars = {'\\', '/', ':', '"', '<', '>', '|', '[', ']', '.'};

        private static string CurrentUserName
        {
            get
            {
                string currentUserName = User.Current.LocalName;
                foreach (var invalidChar in invalidChars)
                {
                    currentUserName = currentUserName.Replace(invalidChar, '_');
                }
                return currentUserName;
            }
        }

        private static string CurrentDomain
        {
            get
            {
                string currentUserDomain = User.Current.Domain.Name;
                foreach (var invalidChar in invalidChars)
                {
                    currentUserDomain = currentUserDomain.Replace(invalidChar, '_');
                }
                return currentUserDomain;
            }
        }

        public static ApplicationSettings GetInstance(string applicationName)
        {
            return GetInstance(applicationName, true);
        }

        internal static void ReloadInstance(string applicationName, bool personalizedSettings)
        {
            string settingsPath = GetSettingsName(applicationName, personalizedSettings);
            lock (instances)
            {
                if (instances.ContainsKey(settingsPath))
                {
                    instances.Remove(settingsPath);
                }
            }
        }

        public static ApplicationSettings GetInstance(string applicationName, bool personalizedSettings)
        {
            string settingsPath = GetSettingsName(applicationName, personalizedSettings);
            ApplicationSettings instance = null;
            lock (instances)
            {
                if (instances.ContainsKey(settingsPath))
                {
                    instance = instances[settingsPath];
                }
                if (instance == null || !instance.Loaded)
                {
                    instance = new ApplicationSettings(applicationName);
                    instance.Load();
                    instances.Add(settingsPath, instance);
                }
            }
            return instance;
        }

        private Item GetSettingsDto()
        {
            Database db = Factory.GetDatabase(SettingsDb);
            return db.GetItem(CurrentUserSettingsPath) ?? db.GetItem(AllUsersSettingsPath);
        }

        private Item GetSettingsDtoForSave()
        {
            Database db = Factory.GetDatabase(SettingsDb);
            string appSettingsPath = AppSettingsPath;
            Item currentUserItem = db.GetItem(CurrentUserSettingsPath);
            if (currentUserItem == null)
            {
                Item settingsRootItem = db.GetItem(appSettingsPath);
                if (settingsRootItem == null)
                {
                    return null;
                }
                Item folderTemplateItem = db.GetItem(FolderTemplatePath);
                Item currentDomainItem = db.CreateItemPath(appSettingsPath + CurrentDomain, folderTemplateItem,
                    folderTemplateItem);
                Item defaultItem = db.GetItem(appSettingsPath + IseSettingsItemAllUsers);
                currentUserItem = defaultItem.CopyTo(currentDomainItem, CurrentUserName);
            }
            return currentUserItem;
        }

        public void Save()
        {
            Item configuration = GetSettingsDtoForSave();
            if (configuration != null)
            {
                configuration.Edit(
                    p =>
                    {
                        configuration["LastScript"] = HttpUtility.HtmlEncode(LastScript);
                        ((CheckboxField) configuration.Fields["SaveLastScript"]).Checked = SaveLastScript;
                        ((CheckboxField) configuration.Fields["UseTypeInfo"]).Checked = UseTypeInfo;
                        configuration["HostWidth"] = HostWidth.ToString(CultureInfo.InvariantCulture);
                        configuration["ForegroundColor"] = ForegroundColor.ToString();
                        configuration["BackgroundColor"] = BackgroundColor.ToString();
                        configuration["FontSize"] = FontSize.ToString();
                        configuration["FontFamily"] = FontFamily;
                    });
            }
        }

        internal void Load()
        {
            Item configuration = GetSettingsDto();

            if (configuration != null)
            {
                Prescript = HttpUtility.HtmlDecode(configuration["PreScript"]);
                LastScript = HttpUtility.HtmlDecode(configuration["LastScript"]);
                SaveLastScript = ((CheckboxField) configuration.Fields["SaveLastScript"]).Checked;
                UseTypeInfo = ((CheckboxField) configuration.Fields["UseTypeInfo"]).Checked;
                int hostWidth;
                HostWidth = Int32.TryParse(configuration["HostWidth"], out hostWidth) ? hostWidth : 150;
                try
                {
                    ForegroundColor = (ConsoleColor) Enum.Parse(typeof (ConsoleColor), configuration["ForegroundColor"]);
                }
                catch (ArgumentException) // disregard parsing error & set default
                {
                    ForegroundColor = ConsoleColor.White;
                }

                try
                {
                    BackgroundColor = (ConsoleColor) Enum.Parse(typeof (ConsoleColor), configuration["BackgroundColor"]);
                }
                catch (ArgumentException) // disregard parsing error & set default
                {
                    BackgroundColor = ConsoleColor.DarkBlue;
                }

                int fontSize;
                FontSize = Int32.TryParse(configuration["FontSize"], out fontSize) ? Math.Max(fontSize, 8) : 12;
                FontFamily = configuration["FontFamily"];
                Loaded = true;
            }
            else
            {
                Prescript = String.Empty;
                LastScript = String.Empty;
                SaveLastScript = false;
                UseTypeInfo = false;
                HostWidth = 80;
                ForegroundColor = ConsoleColor.White;
                BackgroundColor = ConsoleColor.DarkBlue;
                FontSize = 12;
                FontFamily = "Monaco";
                Loaded = true;
            }
        }

    }
}