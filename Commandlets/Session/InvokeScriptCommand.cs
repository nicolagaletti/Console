﻿using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Cognifide.PowerShell.Commandlets.Interactive;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Session
{
    [Cmdlet(VerbsLifecycle.Invoke, "Script", DefaultParameterSetName = "From Content Database Library")]
    [OutputType(new[] {typeof (object)})]
    public class InvokeScriptCommand : BaseShellCommand
    {
        private const string ParameterSetNameFromItem = "From Item";
        private const string ParameterSetNameFromFullPath = "From Full Path";
        private const string ParameterSetNameFromCurrentLocation = "From Current Database Library";
        private const string ParameterSetNameFromContentDatabase = "From Content Database Library";

        [Parameter(ParameterSetName = ParameterSetNameFromItem, ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true, Mandatory = true, Position = 0)]
        public Item Item { get; set; }

        [Parameter(ParameterSetName = ParameterSetNameFromFullPath, Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = ParameterSetNameFromCurrentLocation, Mandatory = true, Position = 0)]
        [Parameter(ParameterSetName = ParameterSetNameFromContentDatabase, Mandatory = true, Position = 0)]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        // Methods
        protected override void ProcessRecord()
        {
            string script = string.Empty;
            if (Item != null)
            {
                script = Item["script"];
            }
            else if (Path != null)
            {
                Item curItem = null;
                if (ParameterSetName == ParameterSetNameFromFullPath ||
                    ParameterSetName == ParameterSetNameFromContentDatabase)
                {
                    curItem = PathUtilities.GetItem(Path, Context.ContentDatabase.Name, ApplicationSettings.ScriptLibraryPath);
                }
                if (curItem == null || ParameterSetName == ParameterSetNameFromCurrentLocation)
                {
                    curItem = PathUtilities.GetItem(Path, CurrentDatabase.Name, ApplicationSettings.ScriptLibraryPath);
                }

                if (curItem == null)
                {
                    WriteError(new ErrorRecord(new ItemNotFoundException(string.Format("Script '{0}' not found.", Path)),
                        "sitecore_script_missing", ErrorCategory.ObjectNotFound, null));
                }
                script = curItem["script"];
            }

            object sendToPipeline = InvokeCommand.InvokeScript(script, false,
                PipelineResultTypes.Output | PipelineResultTypes.Error, null, new object[0]);
            WriteObject(sendToPipeline);
        }
    }
}