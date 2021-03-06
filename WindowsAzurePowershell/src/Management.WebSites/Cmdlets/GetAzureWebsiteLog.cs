﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Management.Websites.Cmdlets
{
    using System;
    using System.Management.Automation;
    using System.Net;
    using System.Web;
    using Common;
    using Microsoft.WindowsAzure.Management.Websites.Utilities;
    using Services;

    /// <summary>
    /// Gets an azure website.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "AzureWebsiteLog"), OutputType(typeof(string))]
    public class GetAzureWebsiteLogCommand : DeploymentBaseCmdlet
    {
        public const int WaitInterval = 10000;

        private const string TailParameterSet = "Tail";

        public RemoteLogStreamManager RemoteLogStreamManager;

        public LogStreamWaitHandle LogStreamWaitHandle;

        public Predicate<string> EndStreaming;

        [Parameter(Position = 1, Mandatory = false, ValueFromPipelineByPropertyName = true, 
            ParameterSetName = TailParameterSet, HelpMessage = "The log path.")]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        [Parameter(Position = 2, Mandatory = false, ValueFromPipelineByPropertyName = true, 
            ParameterSetName = TailParameterSet, HelpMessage = "The search subsrting.")]
        [ValidateNotNullOrEmpty]
        public string Message { get; set; }

        [Parameter(Position = 3, Mandatory = true, ValueFromPipelineByPropertyName = true,
            ParameterSetName = TailParameterSet, HelpMessage = "The log streaming switch.")]
        public SwitchParameter Tail { get; set; }

        /// <summary>
        /// Initializes a new instance of the GetAzureWebsiteLogCommand class.
        /// </summary>
        public GetAzureWebsiteLogCommand()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the GetAzureWebsiteLogCommand class.
        /// </summary>
        /// <param name="channel">
        /// Channel used for communication with Azure's service management APIs.
        /// </param>
        /// <param name="deploymentChannel">
        /// Channel used for communication with the git repository.
        /// </param>
        public GetAzureWebsiteLogCommand(
            IWebsitesServiceManagement channel,
            IDeploymentServiceManagement deploymentChannel)
        {
            Channel = channel;
            DeploymentChannel = deploymentChannel;
        }

        public override void ExecuteCmdlet()
        {
            base.ExecuteCmdlet();

            if (Tail.IsPresent)
            {
                LogStreaming();
            }
        }

        private void LogStreaming()
        {
            ICredentials credentials = new NetworkCredential(
            Repository.PublishingUsername,
            Repository.PublishingPassword);
            Path = HttpUtility.UrlEncode(Path);
            Message = HttpUtility.UrlEncode(Message);

            RemoteLogStreamManager = RemoteLogStreamManager ?? new RemoteLogStreamManager(
                Repository.RepositoryUri,
                Path,
                Message,
                credentials);

            using (LogStreamWaitHandle = LogStreamWaitHandle ?? 
                new LogStreamWaitHandle(RemoteLogStreamManager.GetStream().Result))
            {
                bool doStreaming = true;
                
                while (doStreaming)
                {
                    string line = LogStreamWaitHandle.WaitNextLine(WaitInterval);

                    if (line != null)
                    {
                        WriteObject(line);
                    }

                    doStreaming = EndStreaming == null ? true : EndStreaming(line);
                }
            }
        }
    }
}
