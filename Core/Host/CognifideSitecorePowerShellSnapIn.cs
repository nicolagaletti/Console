﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Xml;
using Cognifide.PowerShell.Core.Provider;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using Pipeline = Sitecore.Pipelines.Pipeline;

namespace Cognifide.PowerShell.Core.Host
{
    [RunInstaller(true)]
    public class CognifideSitecorePowerShellSnapIn : CustomPSSnapIn
    {
        private static readonly List<CmdletConfigurationEntry> commandlets = new List<CmdletConfigurationEntry>();
        private bool initialized;

        static CognifideSitecorePowerShellSnapIn()
        {
            XmlNodeList cmdltsToIncludes = Factory.GetConfigNodes("powershell/commandlets/add");
            foreach (XmlElement cmdltToInclude in cmdltsToIncludes)
            {
                string[] cmdltTypeDef = cmdltToInclude.Attributes["type"].Value.Split(',');
                string cmdletType = cmdltTypeDef[0];
                string cmdletAssembly = cmdltTypeDef[1];
                WildcardPattern wildcard = GetWildcardPattern(cmdletType);
                try
                {
                    Assembly assembly = Assembly.Load(cmdletAssembly);
                    GetCommandletsFromAssembly(assembly, wildcard);
                }
                catch (Exception ex)
                {
                    if (ex is ReflectionTypeLoadException)
                    {
                        var typeLoadException = ex as ReflectionTypeLoadException;
                        Exception[] loaderExceptions = typeLoadException.LoaderExceptions;
                        string message = string.Empty;
                        foreach (var exc in loaderExceptions)
                        {
                            message += exc.Message;
                        }
                        Log.Error("Error while loading commandlets list",ex,typeof(CognifideSitecorePowerShellSnapIn));
                    }
                }
            }
        }

        private static void GetCommandletsFromAssembly(Assembly assembly, WildcardPattern wildcard)
        {
            string helpPath = Path.GetDirectoryName(AppDomain.CurrentDomain.SetupInformation.PrivateBinPath) +
                              @"\sitecore modules\PowerShell\Assets\Cognifide.PowerShell.dll-Help.xml";
            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof (CmdletAttribute), true).Length > 0 &&
                    wildcard.IsMatch(type.FullName))
                {
                    var attribute = (CmdletAttribute) (type.GetCustomAttributes(typeof (CmdletAttribute), true)[0]);
                    Commandlets.Add(new CmdletConfigurationEntry(attribute.VerbName + "-" + attribute.NounName, type,
                        helpPath));
                }
            }
        }


        protected static WildcardPattern GetWildcardPattern(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                name = "*";
            }
            const WildcardOptions options = WildcardOptions.IgnoreCase | WildcardOptions.Compiled;
            var wildcard = new WildcardPattern(name, options);
            return wildcard;
        }


        /// <summary>
        ///     Specify the name of the PowerShell snap-in.
        /// </summary>
        public override string Name
        {
            get { return "CognifideSitecorePowerShellSnapIn"; }
        }

        /// <summary>
        ///     Specify the vendor for the PowerShell snap-in.
        /// </summary>
        public override string Vendor
        {
            get { return "Cognifide"; }
        }

        /// <summary>
        ///     Specify the localization resource information for the vendor.
        ///     Use the format: resourceBaseName,VendorName.
        /// </summary>
        public override string VendorResource
        {
            get { return "CognifideSitecorePowerShellSnapIn,Cognifide"; }
        }

        /// <summary>
        ///     Specify a description of the PowerShell snap-in.
        /// </summary>
        public override string Description
        {
            get { return "This snap-in integrates Sitecore & Powershell."; }
        }

        /// <summary>
        ///     Specify the localization resource information for the description.
        ///     Use the format: resourceBaseName,Description.
        /// </summary>
        public override string DescriptionResource
        {
            get { return "CognifideSitecorePowerShellSnapIn,This snap-in integrates Sitecore & Powershell."; }
        }

        /// <summary>
        ///     Specify the cmdlets that belong to this custom PowerShell snap-in.
        /// </summary>
        public override Collection<CmdletConfigurationEntry> Cmdlets
        {
            get { return new Collection<CmdletConfigurationEntry>(commandlets); }
        }

        /// <summary>
        ///     Specify the providers that belong to this custom PowerShell snap-in.
        /// </summary>
        private Collection<ProviderConfigurationEntry> _providers;

        public override Collection<ProviderConfigurationEntry> Providers
        {
            get
            {
                if (!initialized)
                {
                    Initialize();
                }
                if (_providers == null)
                {
                    _providers = new Collection<ProviderConfigurationEntry>();
                    _providers.Add(new ProviderConfigurationEntry("Sitecore PowerShell Provider",
                        typeof (PsSitecoreItemProvider), @"..\sitecore modules\PowerShell\Assets\Cognifide.PowerShell.dll-Help.xml"));
                }

                return _providers;
            }
        }

        private void Initialize()
        {
            initialized = true;
            Pipeline.Start("initialize", new PipelineArgs(), true);
        }

        public static List<CmdletConfigurationEntry> Commandlets
        {
            get { return commandlets; }
        }
    }
}