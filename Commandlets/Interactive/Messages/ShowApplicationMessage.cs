﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Framework;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Commandlets.Interactive.Messages
{
    [Serializable]
    public class ShowApplicationMessage : IMessage
    {
        public List<BaseListViewCommand.DataObject> Data { get; set; }
        public Hashtable Parameters { get; set; }
        public string AppName { get; private set; }
        public string Title { get; set; }
        public string Width { get; private set; }
        public string Height { get; private set; }
        public string Icon { get; private set; }
        public bool Modal { get; set; }


        public ShowApplicationMessage(string application, string title, string icon, string width, string height,
            bool modal, Hashtable parameters)
        {
            Title = title;
            Width = width;
            Height = height;
            Icon = icon;
            Modal = modal;
            Parameters = parameters;
            AppName = application;
        }


        public void Execute()
        {
            if (!Modal)
            {
                var urlString = new UrlString();
                AddParameters(urlString);
                Item appItem =
                    Database.GetDatabase("core").GetItem("/sitecore/content/Applications/" + AppName);
                Windows.RunApplication(appItem, Icon ?? appItem["Icon"], Title ?? appItem["Display name"],
                    urlString.Query);
            }
            else
            {
                var urlString = new UrlString("/sitecore/shell/sitecore/content/Applications/" + AppName + ".aspx");
                AddParameters(urlString);
                SheerResponse.ShowModalDialog(urlString.ToString(), Width, Height);
            }
        }

        private void AddParameters(UrlString urlString)
        {
            if (Parameters != null)
            {
                foreach (string key in Parameters.Keys)
                {
                    urlString.Add(key, HttpUtility.UrlPathEncode(Parameters[key].ToString()));
                }
            }
        }
    }
}