﻿using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsCommon.Remove, "ItemLanguage")]
    public class RemoveItemLanguageCommand : BaseItemCommand
    {
        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Alias("Languages")]
        [Parameter(ParameterSetName = "Item from Path", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from ID", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from Pipeline", Mandatory = true)]
        public override string[] Language { get; set; }

        [Alias("ExcludeLanguages")]
        [Parameter(ParameterSetName = "Item from Path", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from ID", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from Pipeline", Mandatory = true)]
        public virtual string[] ExcludeLanguage { get; set; }

        protected List<WildcardPattern> ExcludeLanguageWildcardPatterns { get; private set; }

        protected override void BeginProcessing()
        {
            if (ExcludeLanguage == null || !ExcludeLanguage.Any())
            {
                ExcludeLanguageWildcardPatterns = new List<WildcardPattern>();
            }
            else
            {
                ExcludeLanguageWildcardPatterns =
                    ExcludeLanguage.Select(
                        language =>
                            new WildcardPattern(language, WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant))
                        .ToList();
                if (Language == null || !Language.Any())
                {
                    Language = new[] {"*"};
                }
            }
            base.BeginProcessing();
        }

        protected override void ProcessItemLanguages(Item item)
        {
            foreach (Item langItem in item.Versions.GetVersions(true))
            {
                if (LanguageWildcardPatterns.Any(wildcard => wildcard.IsMatch(langItem.Language.Name)))
                {
                    if (!ExcludeLanguageWildcardPatterns.Any(wildcard => wildcard.IsMatch(langItem.Language.Name)))
                    {
                        langItem.Versions.RemoveAll(false);
                    }
                }
            }
            if (Recurse)
            {
                foreach (Item child in item.Children)
                {
                    ProcessItemLanguages(child);
                }
            }
        }

        protected override void ProcessItem(Item item)
        {
            // this function is not used due to override on ProcessItemLanguages
        }

    }

}