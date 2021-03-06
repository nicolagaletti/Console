﻿using System;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Commandlets.Interactive.Messages
{
    [Serializable]
    public class ShellCommandInItemContextMessage : BasePipelineMessage, IMessage, IMessageWithResult
    {
        private readonly string itemUri;
        private readonly string itemDb;
        private readonly string command;
        private Handle jobHandle;

        [NonSerialized]
        private readonly MessageQueue messageQueue;

        public MessageQueue MessageQueue
        {
            get { return messageQueue; }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:Sitecore.Jobs.AsyncUI.ConfirmMessage" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public ShellCommandInItemContextMessage(Item item, string command)
        {
            if (JobContext.IsJob)
            {
                jobHandle = JobContext.JobHandle;
            }

            messageQueue = new MessageQueue();
            if (item != null)
            {
                itemUri = item.Uri.ToDataUri().ToString();
                itemDb = item.Database.Name;
            }
            jobHandle = JobContext.JobHandle;
            this.command = command;
        }

        /// <summary>
        ///     Shows a confirmation dialog.
        /// </summary>
        protected override void ShowUI()
        {
            CommandContext context = null;
            if (!string.IsNullOrEmpty(itemUri))
            {
                Item item = Factory.GetDatabase(itemDb).GetItem(new DataUri(itemUri));
                context = new CommandContext(item);
            }
            else
            {
                context = new CommandContext();
            }
            context.Parameters.Add(Message.Parse(null, command).Arguments);
            if (jobHandle != null)
            {
                context.Parameters.Add("jobHandle", jobHandle.ToString());
            }
            Command shellCommand = CommandManager.GetCommand(command);
            if (shellCommand == null)
                return;
            shellCommand.Execute(context);
        }

    }
}