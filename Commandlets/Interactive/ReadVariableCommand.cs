﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Interactive.Messages;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace Cognifide.PowerShell.Commandlets.Interactive
{
    [Cmdlet(VerbsCommunications.Read, "Variable")]
    [OutputType(new[] {typeof (string)})]
    public class ReadVariableCommand : BaseFormCommand
    {
        [Parameter]
        public object[] Parameters { get; set; }

        [Parameter]
        public string Description { get; set; }

        [Parameter]
        public string CancelButtonName { get; set; }

        [Parameter]
        public string OkButtonName { get; set; }

        [Parameter]
        public SwitchParameter ShowHints { get; set; }

        protected override void ProcessRecord()
        {
            LogErrors(() =>
            {
                AssertDefaultSize(500,300);
                var message = new ShowMultiValuePromptMessage(Parameters, WidthString, HeightString, Title, Description,
                    OkButtonName, CancelButtonName, ShowHints);

                foreach (Hashtable result in Parameters)
                {
                    var name = result["Name"] as string;

                    PSVariable variable = null;
                    if (!string.IsNullOrEmpty(name))
                    {
                        variable = SessionState.PSVariable.Get((string) result["Name"]);
                    }

                    if (result["Variable"] != null)
                    {
                        variable = (PSVariable) ((PSObject) result["Variable"]).BaseObject;
                        result.Add("Name", variable.Name);
                        name = variable.Name;
                    }

                    if (variable != null)
                    {
                        object varValue = result["Value"];
                        if (varValue == null)
                        {
                            varValue = variable.Value.BaseObject();

                            if (varValue is IEnumerable<object>)
                            {
                                varValue = (varValue as IEnumerable<object>).Select(p => PowerShellExtensions.BaseObject(p)).ToList();
                            }
                            result.Add("Value", varValue);
                        }
                        object varTitle = result["Title"];
                        if (varTitle == null)
                        {
                            result.Add("Title", string.IsNullOrEmpty(variable.Name) ? name : variable.Name);
                        }
                        object varDesc = result["Description"];
                        if (varDesc == null)
                        {
                            result.Add("Description", variable.Description);
                        }
                    }

                    if (result["Value"] == null)
                    {
                        if (result.ContainsKey("Value"))
                        {
                            result["Value"] = string.Empty;
                        }
                        else
                        {
                            result.Add("Value", string.Empty);
                        }
                    }
                }

                PutMessage(message);
                var results = (object[]) GetResult(message);
                WriteObject(results != null ? "ok" : "cancel");
                if (results != null)
                {
                    foreach (Hashtable result in results)
                    {
                        object resultValue = result["Value"];
                        if (resultValue is Item)
                        {
                            resultValue = ItemShellExtensions.GetPsObject(SessionState, resultValue as Item);
                        }
                        if (resultValue is List<Item>)
                        {
                            resultValue =
                                (resultValue as List<Item>).Where(p => p != null)
                                    .Select(p => ItemShellExtensions.GetPsObject(SessionState, p))
                                    .ToArray();
                        }
                        SessionState.PSVariable.Set((string) result["Name"], resultValue);
                    }
                }
            });
        }
    }
}