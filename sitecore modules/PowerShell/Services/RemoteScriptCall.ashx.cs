﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Modules;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Security.Authentication;

namespace Cognifide.PowerShell.Console.Services
{
    /// <summary>
    /// The handler allows for execution of scripts stored within Script Library
    /// it also allows those scripts to be parametrized.
    /// </summary>
    public class RemoteScriptCall : IHttpHandler, IRequiresSessionState
    {
        private SortedDictionary<string, SortedDictionary<string,ApiScript>> apiScripts;

        public class ApiScript
        {
            public string Path { get; set; }
            public string Database { get; set; }
            public ID Id { get; set; }
        }

        public RemoteScriptCall()
        {
            ModuleManager.OnInvalidate += InvalidateCache;
            apiScripts = null;
        }

        public void InvalidateCache(object sender, EventArgs e)
        {
            apiScripts = null;
        }

        private void UpdateCache(string dbName)
        {
            if (apiScripts == null)
            {
                apiScripts = new SortedDictionary<string, SortedDictionary<string, ApiScript>>();
                var roots = ModuleManager.GetFeatureRoots(IntegrationPoints.WebApi);
                BuildCache(roots);
            }

            if (!apiScripts.ContainsKey(dbName))
            {
                apiScripts.Add(dbName, new SortedDictionary<string, ApiScript>());
                var roots = ModuleManager.GetFeatureRoots(IntegrationPoints.WebApi, dbName);
                BuildCache(roots);
            }
        }

        private void BuildCache(List<Item> roots)
        {
            foreach (var root in roots)
            {
                var path = PathUtilities.PreparePathForQuery(root.Paths.Path);
                var rootPath = root.Paths.Path;
                var query = string.Format(
                    "{0}//*[@@TemplateId=\"{{DD22F1B3-BD87-4DB2-9E7D-F7A496888D43}}\"]",
                    path);
                try
                {
                    var results = root.Database.SelectItems(query);
                    foreach (var result in results)
                    {
                        var scriptPath = result.Paths.Path.Substring(rootPath.Length);
                        string dbName = result.Database.Name;
                        if (!apiScripts.ContainsKey(dbName))
                        {
                            apiScripts.Add(dbName, new SortedDictionary<string, ApiScript>());
                        }
                        apiScripts[dbName].Add(scriptPath, new ApiScript
                        {
                            Database = result.Database.Name,
                            Id = result.ID,
                            Path = scriptPath
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error while querying for items", ex);
                }
            }
        }

        public void ProcessRequest(HttpContext context)
        {
            var request = HttpContext.Current.Request;
            string userName = request.Params.Get("user");
            string password = request.Params.Get("password");
            string scriptParam = request.Params.Get("script");
            string scriptDbParam = request.Params.Get("scriptDb");
            string apiVersion = request.Params.Get("apiVersion");

            switch (apiVersion)
            {
                case "1":
                    if (!WebServiceSettings.ServiceEnabledRestfulv1)
                    {
                        HttpContext.Current.Response.StatusCode = 403;
                        return;
                    }
                    break;
                case "2":
                    if (!WebServiceSettings.ServiceEnabledRestfulv2)
                    {
                        HttpContext.Current.Response.StatusCode = 403;
                        return;
                    }
                    break;
                default:
                    HttpContext.Current.Response.StatusCode = 403;
                    return;
            }

            bool authenticated = !string.IsNullOrEmpty(userName) &&
                                 !string.IsNullOrEmpty(password) &&
                                 AuthenticationManager.Login(userName, password, false);

            authenticated = Context.IsLoggedIn;
            Database scriptDb =
                !authenticated || string.IsNullOrEmpty(scriptDbParam) || scriptDbParam == "current"
                    ? Context.Database
                    : Database.GetDatabase(scriptDbParam);
            var dbName = scriptDb.Name;

            Item scriptItem = null;

            switch (apiVersion)
            {
                case "1":
                    scriptItem = scriptDb.GetItem(scriptParam) ??
                                 scriptDb.GetItem(ApplicationSettings.ScriptLibraryPath + scriptParam);
                    break;
                case "2":
                default:
                    UpdateCache(dbName);
                    if (!apiScripts.ContainsKey(dbName))
                    {
                        HttpContext.Current.Response.StatusCode = 404;
                        return;
                    }
                    var dbScripts = apiScripts[scriptDb.Name];
                    if (!dbScripts.ContainsKey(scriptParam))
                    {
                        HttpContext.Current.Response.StatusCode = 404;
                        return;
                    }
                    scriptItem = scriptDb.GetItem(dbScripts[scriptParam].Id);
                    break;
            }

            if (scriptItem == null || scriptItem.Fields[ScriptItemFieldNames.Script] == null)
            {
                HttpContext.Current.Response.StatusCode = 404;
                return;
            }

            using (var session = ScriptSessionManager.NewSession(ApplicationNames.Default, true))
            {
                String script = scriptItem.Fields[ScriptItemFieldNames.Script].Value;

                if (Context.Database != null)
                {
                    Item item = Context.Database.GetRootItem();
                    if (item != null)
                        session.SetItemLocationContext(item);
                }
                session.SetExecutedScript(scriptItem);

                context.Response.ContentType = "text/plain";

                foreach (var param in HttpContext.Current.Request.Params.AllKeys)
                {
                    var paramValue = HttpContext.Current.Request.Params[param];
                    if (session.GetVariable(param) == null)
                    {
                        session.SetVariable(param, paramValue);
                    }
                }

                session.ExecuteScriptPart(script, true);

                context.Response.Write(session.Output.ToString());

                if (session.Output.HasErrors)
                {
                    context.Response.StatusCode = 424;
                    context.Response.StatusDescription = "Method Failure";
                }
            }
        }

        public bool IsReusable
        {
            get { return true; }
        }
    }
}