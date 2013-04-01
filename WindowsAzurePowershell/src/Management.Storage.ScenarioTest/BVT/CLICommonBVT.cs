// ----------------------------------------------------------------------------------
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CLITest.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using MS.Test.Common.MsTestLib;
using StorageTestLib;
using Microsoft.WindowsAzure.Management.ScenarioTest.Common;
using System.Management.Automation;
using Storage = Microsoft.WindowsAzure.Storage.Blob;

namespace CLITest.BVT
{
    /// <summary>
    /// this class contain all the bvt cases for the full functional storage context such as local/connectionstring/namekey, anonymous and sas token are excluded.
    /// </summary>
    [TestClass]
    public class CLICommonBVT: WindowsAzurePowerShellTest
    {
        private static CloudBlobHelper CommonBlobHelper;
        private static CloudStorageAccount CommonStorageAccount;
        private static string CommonBlockFilePath;
        private static string CommonPageFilePath;

        //env connection string
        private static string SavedEnvString;
        public static string EnvKey;

        /// <summary>
        /// the storage account which is used to set up the unit tests.
        /// </summary>
        protected static CloudStorageAccount SetUpStorageAccount
        {
            get 
            {
                return CommonStorageAccount;
            }

            set
            {
                CommonStorageAccount = value;
            }
        }

        #region Additional test attributes

        public CLICommonBVT()
        { 
        }
        
        /// <summary>
        /// Init test resources for bvt class
        /// </summary>
        /// <param name="testContext">TestContext object</param>
        [ClassInitialize()]
        public static void CLICommonBVTInitialize(TestContext testContext)
        {
            Test.Info(string.Format("{0} Class  Initialize", testContext.FullyQualifiedTestClassName));
            Test.FullClassName = testContext.FullyQualifiedTestClassName;
            EnvKey = Test.Data.Get("EnvContextKey");
            SaveAndCleanSubScriptionAndEnvConnectionString();

            //Clean Storage Context
            Test.Info("Clean storage context in PowerShell");
            PowerShellAgent.CleanStorageContext();

            PowerShellAgent.ImportModule(@".\Microsoft.WindowsAzure.Management.Storage.dll");
            

            // import module
            string moduleFilePath = Test.Data.Get("ModuleFilePath");
            PowerShellAgent.ImportModule(moduleFilePath);

            GenerateBvtTempFiles();
        }

        /// <summary>
        /// Save azure subscription and env connection string. So the current settings can't impact our tests.
        /// </summary>
        //TODO move to TestBase
        public static void SaveAndCleanSubScriptionAndEnvConnectionString()
        {
            Test.Info("Clean Azure Subscription and save env connection string");
            //can't restore the azure subscription files
            PowerShellAgent.RemoveAzureSubscriptionIfExists();

            //set env connection string
            //TODO A little bit trivial, move to CLITestBase class
            if (string.IsNullOrEmpty(EnvKey))
            {
                EnvKey = Test.Data.Get("EnvContextKey");
            }

            SavedEnvString = System.Environment.GetEnvironmentVariable(EnvKey);
            System.Environment.SetEnvironmentVariable(EnvKey, string.Empty);
        }

        /// <summary>
        /// Restore the previous subscription and env connection string before testing.
        /// </summary>
        public static void RestoreSubScriptionAndEnvConnectionString()
        {
            Test.Info("Restore env connection string and skip restore subscription");
            System.Environment.SetEnvironmentVariable(EnvKey, SavedEnvString);
        }

        /// <summary>
        /// Generate temp files
        /// </summary>
        private static void GenerateBvtTempFiles()
        {
            CommonBlockFilePath = Test.Data.Get("BlockFilePath");
            CommonPageFilePath = Test.Data.Get("PageFilePath");
            string downloadDir = Test.Data.Get("DownloadDirPath");

            CreateDirIfNotExits(Path.GetDirectoryName(CommonBlockFilePath));
            CreateDirIfNotExits(Path.GetDirectoryName(CommonPageFilePath));
            CreateDirIfNotExits(downloadDir);

            // Generate block file and page file which are used for uploading
            Helper.GenerateMediumFile(CommonBlockFilePath, 1);
            Helper.GenerateMediumFile(CommonPageFilePath, 1);
        }

        private static void CreateDirIfNotExits(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }

        /// <summary>
        /// Clean up test resources of  bvt class
        /// </summary>
        [ClassCleanup()]
        public static void CLICommonBVTCleanup()
        {
            Test.Info(string.Format("BVT Test Class Cleanup"));
            RestoreSubScriptionAndEnvConnectionString();
        }

        /// <summary>
        /// init test resources for one single unit test.
        /// </summary>
        [TestInitialize()]
        public void StorageTestInitialize()
        {
            SetTestStorageAccount(powershell);
            PowerShellAgent.SetPowerShellInstance(powershell);
            Test.Start(TestContext.FullyQualifiedTestClassName, TestContext.TestName);
        }

        private string EnvConnectionStringInPowerShell;

        private void SetTestStorageAccount(PowerShell powershell)
        {
            if (String.IsNullOrEmpty(EnvConnectionStringInPowerShell))
            {
                PSCommand currentCommand = powershell.Commands.Clone();
                string envConnStringScript = string.Format("$env:{0}", Test.Data.Get("EnvContextKey"));
                powershell.AddScript(envConnStringScript);
                Collection<PSObject> output = powershell.Invoke();

                if (output.Count == 1)
                {
                    EnvConnectionStringInPowerShell = output[0].BaseObject.ToString();
                    powershell.Commands = currentCommand;
                }
                else
                {
                    Test.AssertFail("Can not find the environment variable 'AZURE_STORAGE_CONNECTION_STRING' in powershell instance");
                }
            }

            if (String.IsNullOrEmpty(EnvConnectionStringInPowerShell))
            {
                throw new ArgumentException("Please set the StorageConnectionString element of TestData.xml");
            }

            CommonStorageAccount = CloudStorageAccount.Parse(EnvConnectionStringInPowerShell);

            CommonBlobHelper = new CloudBlobHelper(CommonStorageAccount);
        }

        /// <summary>
        /// clean up the test resources for one single unit test.
        /// </summary>
        [TestCleanup()]
        public void StorageTestCleanup()
        {
            Trace.WriteLine("Unit Test Cleanup");
            Test.End(TestContext.FullyQualifiedTestClassName, TestContext.TestName);
        }

        #endregion

        /// <summary>
        /// BVT case : for New-AzureStorageContainer
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.FastEnv)]
        [TestCategory(Category.All)]
        [TestCategory(Category.Storage)]
        [TestCategory("Scenario")]
        public void NewContainerTest()
        {
            NewContainerTest(new PowerShellAgent());
        }

        /// <summary>
        /// BVT case : for Get-AzureStorageContainer
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.FastEnv)]
        [TestCategory(Category.All)]
        [TestCategory(Category.Storage)]
        [TestCategory("Scenario")]
        public void GetContainerTest()
        {
            GetContainerTest(new PowerShellAgent());
        }

        /// <summary>
        /// BVT case : for Remove-AzureStorageContainer
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.FastEnv)]
        [TestCategory(Category.All)]
        [TestCategory(Category.Storage)]
        [TestCategory("Scenario")]
        public void RemoveContainerTest()
        {
            RemoveContainerTest(new PowerShellAgent());
        }

        /// <summary>
        /// BVT case : for Set-AzureStorageContainerACL
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.FastEnv)]
        [TestCategory(Category.All)]
        [TestCategory(Category.Storage)]
        [TestCategory("Scenario")]
        public void SetContainerACLTest()
        {
            SetContainerACLTest(new PowerShellAgent());
        }

        /// <summary>
        /// BVT case : for Set-AzureStorageBlobContent
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.FastEnv)]
        [TestCategory(Category.All)]
        [TestCategory(Category.Storage)]
        [TestCategory("Scenario")]
        public void UploadBlobTest()
        {
            UploadBlobTest(new PowerShellAgent(), CommonBlockFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob);
            UploadBlobTest(new PowerShellAgent(), CommonPageFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType.PageBlob);
        }

        /// <summary>
        /// BVT case : for Get-AzureStorageBlob
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.FastEnv)]
        [TestCategory(Category.All)]
        [TestCategory(Category.Storage)]
        [TestCategory("Scenario")]
        public void GetBlobTest()
        {
            GetBlobTest(new PowerShellAgent(), CommonBlockFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob);
            GetBlobTest(new PowerShellAgent(), CommonPageFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType.PageBlob);
        }

        /// <summary>
        /// BVT case : for Get-AzureStorageBlobContent
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.FastEnv)]
        [TestCategory(Category.All)]
        [TestCategory(Category.Storage)]
        [TestCategory("Scenario")]
        public void DownloadBlobTest()
        {
            string downloadDirPath = Test.Data.Get("DownloadDirPath");
            DownloadBlobTest(new PowerShellAgent(), CommonBlockFilePath, downloadDirPath, Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob);
            DownloadBlobTest(new PowerShellAgent(), CommonPageFilePath, downloadDirPath, Microsoft.WindowsAzure.Storage.Blob.BlobType.PageBlob);
        }

        /// <summary>
        /// BVT case : for Remove-AzureStorageBlob
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.FastEnv)]
        [TestCategory(Category.All)]
        [TestCategory(Category.Storage)]
        [TestCategory("Scenario")]
        public void RemoveBlobTest()
        {
            RemoveBlobTest(new PowerShellAgent(), CommonBlockFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob);
            RemoveBlobTest(new PowerShellAgent(), CommonPageFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType.PageBlob);
        }

        /// <summary>
        /// BVT case : for Start-CopyAzureStorageBlob
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.FastEnv)]
        [TestCategory(Category.All)]
        [TestCategory(Category.Storage)]
        [TestCategory("Scenario")]
        public void StartCopyBlobUsingName()
        {
            StartCopyBlobTest(new PowerShellAgent(), false);
        }

        /// <summary>
        /// BVT case : for Start-CopyAzureStorageBlob
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.FastEnv)]
        [TestCategory(Category.All)]
        [TestCategory(Category.Storage)]
        [TestCategory("Scenario")]
        public void StartCopyBlobUsingUri()
        {
            StartCopyBlobTest(new PowerShellAgent(), true);
        }

        /// <summary>
        /// BVT case : for Get-AzureStorageBlobCopyState
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.FastEnv)]
        [TestCategory(Category.All)]
        [TestCategory(Category.Storage)]
        [TestCategory("Scenario")]
        public void GetBlobCopyStateTest()
        {
            CloudBlobUtil blobUtil = new CloudBlobUtil(CommonStorageAccount);
            blobUtil.SetupTestContainerAndBlob();
            ICloudBlob destBlob = CopyBlobAndWaitForComplete(blobUtil);

            try
            {
                Test.Assert(destBlob.CopyState.Status == CopyStatus.Success, String.Format("The blob copy using storage client should be success, actually it's {0}", destBlob.CopyState.Status));

                PowerShellAgent agent = new PowerShellAgent();
                Test.Assert(agent.GetAzureStorageBlobCopyState(blobUtil.ContainerName, destBlob.Name, false), "Get copy state should be success");
                int expectedStateCount = 1;
                Test.Assert(agent.Output.Count == expectedStateCount, String.Format("Expected to get {0} copy state, actually it's {1}", expectedStateCount, agent.Output.Count));
                CopyStatus copyStatus = (CopyStatus)agent.Output[0]["Status"];
                Test.Assert(copyStatus == CopyStatus.Success, String.Format("The blob copy should be success, actually it's {0}", copyStatus));
                Uri sourceUri = (Uri)agent.Output[0]["Source"];
                string expectedUri = CloudBlobUtil.ConvertCopySourceUri(blobUtil.Blob.Uri.ToString());
                Test.Assert(sourceUri.ToString() == expectedUri, String.Format("Expected source uri is {0}, actully it's {1}", expectedUri, sourceUri.ToString()));

                Test.Assert(!agent.GetAzureStorageBlobCopyState(blobUtil.ContainerName, blobUtil.BlobName, false), "Get copy state should be fail since the specified blob don't have any copy operation");
                Test.Assert(agent.ErrorMessages.Count > 0, "Should return error message");
                string errorMessage = "Can not find copy task on specified blob";
                Test.Assert(agent.ErrorMessages[0].StartsWith(errorMessage), String.Format("Error message should start with {0}, and actually it's {1}", errorMessage, agent.ErrorMessages[0]));
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        /// <summary>
        /// BVT case : for Stop-CopyAzureStorageBlob
        /// </summary>
        [TestMethod]
        [TestCategory(Tag.BVT)]
        [TestCategory(PsTag.FastEnv)]
        [TestCategory(Category.All)]
        [TestCategory(Category.Storage)]
        [TestCategory("Scenario")]
        public void StopCopyBlobTest()
        {
            CloudBlobUtil blobUtil = new CloudBlobUtil(CommonStorageAccount);
            blobUtil.SetupTestContainerAndBlob();
            ICloudBlob destBlob = CopyBlobAndWaitForComplete(blobUtil);

            try
            {
                PowerShellAgent agent = new PowerShellAgent();
                string copyId = Guid.NewGuid().ToString();
                Test.Assert(!agent.StopCopyAzureStorageBlob(blobUtil.ContainerName, blobUtil.BlobName, copyId, true), "Stop copy operation should be fail since the specified blob don't have any copy operation");
                Test.Assert(agent.ErrorMessages.Count > 0, "Should return error message");
                string errorMessage = String.Format("Can not find copy task on specified blob '{0}' in container '{1}'", blobUtil.BlobName, blobUtil.ContainerName);
                Test.Assert(agent.ErrorMessages[0].IndexOf(errorMessage) != -1, String.Format("Error message should contain {0}, and actually it's {1}", errorMessage, agent.ErrorMessages[0]));

                errorMessage = "There is currently no pending copy operation.";
                Test.Assert(!agent.StopCopyAzureStorageBlob(blobUtil.ContainerName, destBlob.Name, copyId, true), "Stop copy operation should be fail since the specified copy operation has finished");
                Test.Assert(agent.ErrorMessages.Count > 0, "Should return error message");
                Test.Assert(agent.ErrorMessages[0].IndexOf(errorMessage) != -1, String.Format("Error message should contain {0}, and actually it's {1}", errorMessage, agent.ErrorMessages[0]));
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }


        internal ICloudBlob CopyBlobAndWaitForComplete(CloudBlobUtil blobUtil)
        {
            string destBlobName = Utility.GenNameString("copystate");

            ICloudBlob destBlob = default(ICloudBlob);

            Test.Info("Copy Blob using storage client");

            if (blobUtil.Blob.BlobType == Storage.BlobType.BlockBlob)
            {
                CloudBlockBlob blockBlob = blobUtil.Container.GetBlockBlobReference(destBlobName);
                blockBlob.StartCopyFromBlob((CloudBlockBlob)blobUtil.Blob);
                destBlob = blockBlob;
            }
            else
            {
                CloudPageBlob pageBlob = blobUtil.Container.GetPageBlobReference(destBlobName);
                pageBlob.StartCopyFromBlob((CloudPageBlob)blobUtil.Blob);
                destBlob = pageBlob;
            }

            CloudBlobUtil.WaitForCopyOperationComplete(destBlob);

            Test.Assert(destBlob.CopyState.Status == CopyStatus.Success, String.Format("The blob copy using storage client should be success, actually it's {0}", destBlob.CopyState.Status));

            return destBlob;
        }

        internal void StartCopyBlobTest(Agent agent, bool useUri)
        {
            CloudBlobUtil blobUtil = new CloudBlobUtil(CommonStorageAccount);
            blobUtil.SetupTestContainerAndBlob();
            string copiedName = Utility.GenNameString("copied");

            if (useUri)
            {
                //Set the blob permission, so the copy task could directly copy by uri
                BlobContainerPermissions permission = new BlobContainerPermissions();
                permission.PublicAccess = BlobContainerPublicAccessType.Blob;
                blobUtil.Container.SetPermissions(permission);
            }

            try
            {
                if (useUri)
                {
                    Test.Assert(agent.StartCopyAzureStorageBlob(blobUtil.Blob.Uri.ToString(), blobUtil.ContainerName, copiedName, PowerShellAgent.Context), Utility.GenComparisonData("Start copy blob using source uri", true));
                }
                else
                {
                    Test.Assert(agent.StartCopyAzureStorageBlob(blobUtil.ContainerName, blobUtil.BlobName, blobUtil.ContainerName, copiedName), Utility.GenComparisonData("Start copy blob using blob name", true));
                }

                Test.Info("Get destination blob in copy task");
                ICloudBlob blob = blobUtil.Container.GetBlobReferenceFromServer(copiedName);
                Test.Assert(blob != null, "Destination blob should exist after start copy. If not, please check it's a test issue or dev issue.");

                string sourceUri = CloudBlobUtil.ConvertCopySourceUri(blobUtil.Blob.Uri.ToString());

                Test.Assert(blob.BlobType == blobUtil.Blob.BlobType, String.Format("The destination blob type should be {0}, actually {1}.", blobUtil.Blob.BlobType, blob.BlobType));

                Test.Assert(blob.CopyState.Source.ToString().StartsWith(sourceUri), String.Format("The source of destination blob should start with {0}, and actually it's {1}", sourceUri, blob.CopyState.Source.ToString()));
            }
            finally
            {
                blobUtil.CleanupTestContainerAndBlob();
            }
        }

        internal void NewContainerTest(Agent agent)
        {
            string NEW_CONTAINER_NAME = Utility.GenNameString("astoria-");

            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Container, NEW_CONTAINER_NAME);
            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>> { dic };

            // delete container if it exists
            CloudBlobContainer container = CommonStorageAccount.CreateCloudBlobClient().GetContainerReference(NEW_CONTAINER_NAME);
            container.DeleteIfExists();

            try
            {
                //--------------New operation--------------
                Test.Assert(agent.NewAzureStorageContainer(NEW_CONTAINER_NAME), Utility.GenComparisonData("NewAzureStorageContainer", true));
                // Verification for returned values
                CloudBlobUtil.PackContainerCompareData(container, dic);
                agent.OutputValidation(comp);
                Test.Assert(container.Exists(), "container {0} should exist!", NEW_CONTAINER_NAME);
            }
            finally
            {
                // clean up
                container.DeleteIfExists();
            }
        }

        internal void GetContainerTest(Agent agent)
        {
            string NEW_CONTAINER_NAME = Utility.GenNameString("astoria-");

            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Container, NEW_CONTAINER_NAME);

            // create container if it does not exist
            CloudBlobContainer container = CommonStorageAccount.CreateCloudBlobClient().GetContainerReference(NEW_CONTAINER_NAME);
            container.CreateIfNotExists();

            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>> { dic };

            try
            {
                //--------------Get operation--------------
                Test.Assert(agent.GetAzureStorageContainer(NEW_CONTAINER_NAME), Utility.GenComparisonData("GetAzureStorageContainer", true));
                // Verification for returned values
                container.FetchAttributes();
                dic.Add("CloudBlobContainer", container);
                CloudBlobUtil.PackContainerCompareData(container, dic);

                agent.OutputValidation(comp);
            }
            finally
            {
                // clean up
                container.DeleteIfExists();
            }
        }

        internal void RemoveContainerTest(Agent agent)
        {
            string NEW_CONTAINER_NAME = Utility.GenNameString("astoria-");

            // create container if it does not exist
            CloudBlobContainer container = CommonStorageAccount.CreateCloudBlobClient().GetContainerReference(NEW_CONTAINER_NAME);
            container.CreateIfNotExists();

            try
            {
                //--------------Remove operation--------------
                Test.Assert(agent.RemoveAzureStorageContainer(NEW_CONTAINER_NAME), Utility.GenComparisonData("RemoveAzureStorageContainer", true));
                Test.Assert(!container.Exists(), "container {0} should not exist!", NEW_CONTAINER_NAME);
            }
            finally
            {
                // clean up
                container.DeleteIfExists();
            }
        }

        internal void SetContainerACLTest(Agent agent)
        {
            string NEW_CONTAINER_NAME = Utility.GenNameString("astoria-");

            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Container, NEW_CONTAINER_NAME);
            comp.Add(dic);

            // create container if it does not exist
            CloudBlobContainer container = CommonStorageAccount.CreateCloudBlobClient().GetContainerReference(NEW_CONTAINER_NAME);
            container.CreateIfNotExists();

            try
            {
                BlobContainerPublicAccessType[] accessTypes = new BlobContainerPublicAccessType[] { 
                    BlobContainerPublicAccessType.Blob,
                    BlobContainerPublicAccessType.Container,
                    BlobContainerPublicAccessType.Off
                };

                // set PublicAccess as one value respetively
                foreach (var accessType in accessTypes)
                {
                    //--------------Set operation-------------- 
                    Test.Assert(agent.SetAzureStorageContainerACL(NEW_CONTAINER_NAME, accessType),
                        "SetAzureStorageContainerACL operation should succeed");
                    // Verification for returned values
                    dic["PublicAccess"] = accessType;
                    CloudBlobUtil.PackContainerCompareData(container, dic);
                    agent.OutputValidation(comp);

                    Test.Assert(container.GetPermissions().PublicAccess == accessType,
                        "PublicAccess should be equal: {0} = {1}", container.GetPermissions().PublicAccess, accessType);
                }
            }
            finally
            {
                // clean up
                container.DeleteIfExists();
            }
        }

        internal void NewTableTest(Agent agent)
        {
            string NEW_TABLE_NAME = Utility.GenNameString("Washington");
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Table, NEW_TABLE_NAME);
            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>> { dic };

            // delete table if it exists
            CloudTable table = CommonStorageAccount.CreateCloudTableClient().GetTableReference(NEW_TABLE_NAME);
            table.DeleteIfExists();

            try
            {
                //--------------New operation--------------
                Test.Assert(agent.NewAzureStorageTable(NEW_TABLE_NAME), Utility.GenComparisonData("NewAzureStorageTable", true));
                // Verification for returned values
                dic.Add("CloudTable", table);
                agent.OutputValidation(comp);
                Test.Assert(table.Exists(), "table {0} should exist!", NEW_TABLE_NAME);
            }
            finally
            {
                table.DeleteIfExists();
            }
        }

        internal void GetTableTest(Agent agent)
        {
            string NEW_TABLE_NAME = Utility.GenNameString("Washington");
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Table, NEW_TABLE_NAME);
            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>> { dic };

            // create table if it does not exist
            CloudTable table = CommonStorageAccount.CreateCloudTableClient().GetTableReference(NEW_TABLE_NAME);
            table.CreateIfNotExists();

            dic.Add("CloudTable", table);

            try
            {
                //--------------Get operation--------------
                Test.Assert(agent.GetAzureStorageTable(NEW_TABLE_NAME), Utility.GenComparisonData("GetAzureStorageTable", true));
                // Verification for returned values
                agent.OutputValidation(comp);
            }
            finally
            {
                // clean up
                table.DeleteIfExists();
            }
        }

        internal void RemoveTableTest(Agent agent)
        {
            string NEW_TABLE_NAME = Utility.GenNameString("Washington");

            // create table if it does not exist
            CloudTable table = CommonStorageAccount.CreateCloudTableClient().GetTableReference(NEW_TABLE_NAME);
            table.CreateIfNotExists();

            try
            {
                //--------------Remove operation--------------
                Test.Assert(agent.RemoveAzureStorageTable(NEW_TABLE_NAME), Utility.GenComparisonData("RemoveAzureStorageTable", true));
                Test.Assert(!table.Exists(), "queue {0} should not exist!", NEW_TABLE_NAME);
            }
            finally
            {
                // clean up
                table.DeleteIfExists();
            }
        }

        internal void NewQueueTest(Agent agent)
        {
            string NEW_QUEUE_NAME = Utility.GenNameString("redmond-");
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Queue, NEW_QUEUE_NAME);
            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>> { dic };

            CloudQueue queue = CommonStorageAccount.CreateCloudQueueClient().GetQueueReference(NEW_QUEUE_NAME);
            // delete queue if it exists
            queue.DeleteIfExists();

            try
            {
                //--------------New operation--------------
                Test.Assert(agent.NewAzureStorageQueue(NEW_QUEUE_NAME), Utility.GenComparisonData("NewAzureStorageQueue", true));
                queue.FetchAttributes();
                dic.Add("CloudQueue", queue);
                // Verification for returned values               
                agent.OutputValidation(comp);
                Test.Assert(queue.Exists(), "queue {0} should exist!", NEW_QUEUE_NAME);
            }
            finally
            {
                // clean up
                queue.DeleteIfExists();
            }
        }

        internal void GetQueueTest(Agent agent)
        {
            string NEW_QUEUE_NAME = Utility.GenNameString("redmond-");
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Queue, NEW_QUEUE_NAME);
            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>> { dic };

            CloudQueue queue = CommonStorageAccount.CreateCloudQueueClient().GetQueueReference(NEW_QUEUE_NAME);
            // create queue if it does exist
            queue.CreateIfNotExists();

            dic.Add("CloudQueue", queue);
            try
            {
                //--------------Get operation--------------
                Test.Assert(agent.GetAzureStorageQueue(NEW_QUEUE_NAME), Utility.GenComparisonData("GetAzureStorageQueue", true));
                // Verification for returned values
                queue.FetchAttributes();
                agent.OutputValidation(comp);
            }
            finally
            {
                // clean up
                queue.DeleteIfExists();
            }
        }

        internal void RemoveQueueTest(Agent agent)
        {
            string NEW_QUEUE_NAME = Utility.GenNameString("redmond-");

            // create queue if it does exist
            CloudQueue queue = CommonStorageAccount.CreateCloudQueueClient().GetQueueReference(NEW_QUEUE_NAME);
            queue.CreateIfNotExists();

            try
            {
                //--------------Remove operation--------------
                Test.Assert(agent.RemoveAzureStorageQueue(NEW_QUEUE_NAME), Utility.GenComparisonData("RemoveAzureStorageQueue", true));
                Test.Assert(!queue.Exists(), "queue {0} should not exist!", NEW_QUEUE_NAME);
            }
            finally
            {
                // clean up
                queue.DeleteIfExists();
            }
        }

        /// <summary>
        /// Parameters:
        ///     Block:
        ///         True for BlockBlob, false for PageBlob
        /// </summary>
        internal void UploadBlobTest(Agent agent, string UploadFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType Type)
        {
            string NEW_CONTAINER_NAME = Utility.GenNameString("upload-");
            string blobName = Path.GetFileName(UploadFilePath);

            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Blob, blobName);

            dic["BlobType"] = Type;
            comp.Add(dic);

            // create the container
            CloudBlobContainer container = CommonStorageAccount.CreateCloudBlobClient().GetContainerReference(NEW_CONTAINER_NAME);
            container.CreateIfNotExists();

            try
            {
                //--------------Upload operation--------------
                Test.Assert(agent.SetAzureStorageBlobContent(UploadFilePath, NEW_CONTAINER_NAME, Type), Utility.GenComparisonData("SendAzureStorageBlob", true));

                ICloudBlob blob = CommonBlobHelper.QueryBlob(NEW_CONTAINER_NAME, blobName);
                CloudBlobUtil.PackBlobCompareData(blob, dic);
                // Verification for returned values
                agent.OutputValidation(comp);
                Test.Assert(blob != null && blob.Exists(), "blob " + blobName + " should exist!");
            }
            finally
            {
                // cleanup
                container.DeleteIfExists();
            }
        }

        /// <summary>
        /// Parameters:
        ///     Block:
        ///         True for BlockBlob, false for PageBlob
        /// </summary>
        internal void GetBlobTest(Agent agent, string UploadFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType Type)
        {
            string NEW_CONTAINER_NAME = Utility.GenNameString("upload-");
            string blobName = Path.GetFileName(UploadFilePath);

            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Blob, blobName);

            dic["BlobType"] = Type;
            comp.Add(dic);

            // create the container
            CloudBlobContainer container = CommonStorageAccount.CreateCloudBlobClient().GetContainerReference(NEW_CONTAINER_NAME);
            container.CreateIfNotExists();

            try
            {
                bool bSuccess = false;
                // upload the blob file
                if (Type == Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob)
                    bSuccess = CommonBlobHelper.UploadFileToBlockBlob(NEW_CONTAINER_NAME, blobName, UploadFilePath);
                else if (Type == Microsoft.WindowsAzure.Storage.Blob.BlobType.PageBlob)
                    bSuccess = CommonBlobHelper.UploadFileToPageBlob(NEW_CONTAINER_NAME, blobName, UploadFilePath);
                Test.Assert(bSuccess, "upload file {0} to container {1} should succeed", UploadFilePath, NEW_CONTAINER_NAME);

                //--------------Get operation--------------
                Test.Assert(agent.GetAzureStorageBlob(blobName, NEW_CONTAINER_NAME), Utility.GenComparisonData("GetAzureStorageBlob", true));

                // Verification for returned values
                // get blob object using XSCL 
                ICloudBlob blob = CommonBlobHelper.QueryBlob(NEW_CONTAINER_NAME, blobName);
                blob.FetchAttributes();
                CloudBlobUtil.PackBlobCompareData(blob, dic);
                dic.Add("ICloudBlob", blob);

                agent.OutputValidation(comp);
            }
            finally
            {
                // cleanup
                container.DeleteIfExists();
            }
        }

        /// <summary>
        /// Parameters:
        ///     Block:
        ///         True for BlockBlob, false for PageBlob
        /// </summary>
        internal void DownloadBlobTest(Agent agent, string UploadFilePath, string DownloadDirPath, Microsoft.WindowsAzure.Storage.Blob.BlobType Type)
        {
            string NEW_CONTAINER_NAME = Utility.GenNameString("upload-");
            string blobName = Path.GetFileName(UploadFilePath);

            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Blob, blobName);

            dic["BlobType"] = Type;
            comp.Add(dic);

            // create the container
            CloudBlobContainer container = CommonStorageAccount.CreateCloudBlobClient().GetContainerReference(NEW_CONTAINER_NAME);
            container.CreateIfNotExists();

            try
            {
                bool bSuccess = false;
                // upload the blob file
                if (Type == Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob)
                    bSuccess = CommonBlobHelper.UploadFileToBlockBlob(NEW_CONTAINER_NAME, blobName, UploadFilePath);
                else if (Type == Microsoft.WindowsAzure.Storage.Blob.BlobType.PageBlob)
                    bSuccess = CommonBlobHelper.UploadFileToPageBlob(NEW_CONTAINER_NAME, blobName, UploadFilePath);
                Test.Assert(bSuccess, "upload file {0} to container {1} should succeed", UploadFilePath, NEW_CONTAINER_NAME);

                //--------------Download operation--------------
                string downloadFilePath = Path.Combine(DownloadDirPath, blobName);
                Test.Assert(agent.GetAzureStorageBlobContent(blobName, downloadFilePath, NEW_CONTAINER_NAME),
                    Utility.GenComparisonData("GetAzureStorageBlobContent", true));
                ICloudBlob blob = CommonBlobHelper.QueryBlob(NEW_CONTAINER_NAME, blobName);
                CloudBlobUtil.PackBlobCompareData(blob, dic);
                // Verification for returned values
                agent.OutputValidation(comp);

                Test.Assert(Helper.CompareTwoFiles(downloadFilePath, UploadFilePath),
                    String.Format("File '{0}' should be bit-wise identicial to '{1}'", downloadFilePath, UploadFilePath));
            }
            finally
            {
                // cleanup
                container.DeleteIfExists();
            }
        }

        /// <summary>
        /// Parameters:
        ///     Block:
        ///         True for BlockBlob, false for PageBlob
        /// </summary>
        internal void RemoveBlobTest(Agent agent, string UploadFilePath, Microsoft.WindowsAzure.Storage.Blob.BlobType Type)
        {
            string NEW_CONTAINER_NAME = Utility.GenNameString("upload-");
            string blobName = Path.GetFileName(UploadFilePath);

            Collection<Dictionary<string, object>> comp = new Collection<Dictionary<string, object>>();
            Dictionary<string, object> dic = Utility.GenComparisonData(StorageObjectType.Blob, blobName);

            dic["BlobType"] = Type;
            comp.Add(dic);

            // create the container
            CloudBlobContainer container = CommonStorageAccount.CreateCloudBlobClient().GetContainerReference(NEW_CONTAINER_NAME);
            container.CreateIfNotExists();

            try
            {
                bool bSuccess = false;
                // upload the blob file
                if (Type == Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob)
                    bSuccess = CommonBlobHelper.UploadFileToBlockBlob(NEW_CONTAINER_NAME, blobName, UploadFilePath);
                else if (Type == Microsoft.WindowsAzure.Storage.Blob.BlobType.PageBlob)
                    bSuccess = CommonBlobHelper.UploadFileToPageBlob(NEW_CONTAINER_NAME, blobName, UploadFilePath);
                Test.Assert(bSuccess, "upload file {0} to container {1} should succeed", UploadFilePath, NEW_CONTAINER_NAME);

                //--------------Remove operation--------------
                Test.Assert(agent.RemoveAzureStorageBlob(blobName, NEW_CONTAINER_NAME), Utility.GenComparisonData("RemoveAzureStorageBlob", true));
                ICloudBlob blob = CommonBlobHelper.QueryBlob(NEW_CONTAINER_NAME, blobName);
                Test.Assert(blob == null, "blob {0} should not exist!", blobName);
            }
            finally
            {
                // cleanup
                container.DeleteIfExists();
            }
        }

        /// <summary>
        /// Create a container and then get it using powershell cmdlet
        /// </summary>
        /// <returns>A CloudBlobContainer object which is returned by PowerShell</returns>
        protected CloudBlobContainer CreateAndPsGetARandomContainer()
        {
            string containerName = Utility.GenNameString("bvt");
            CloudBlobContainer container = SetUpStorageAccount.CreateCloudBlobClient().GetContainerReference(containerName);
            container.CreateIfNotExists();

            try
            {
                PowerShellAgent agent = new PowerShellAgent();
                Test.Assert(agent.GetAzureStorageContainer(containerName), Utility.GenComparisonData("GetAzureStorageContainer", true));
                int count = 1;
                Test.Assert(agent.Output.Count == count, string.Format("get container should return only 1 container, actully it's {0}", agent.Output.Count));
                return (CloudBlobContainer)agent.Output[0]["CloudBlobContainer"];
            }
            finally
            {
                // clean up
                container.DeleteIfExists();
            }
        }
    }
}
