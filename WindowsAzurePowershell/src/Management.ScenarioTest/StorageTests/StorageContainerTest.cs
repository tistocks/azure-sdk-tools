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

namespace Microsoft.WindowsAzure.Management.ScenarioTest.StorageTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Management.ScenarioTest.Common;
    using Microsoft.WindowsAzure.Management.Storage;

    [TestClass]
    public class StorageContainerTest : WindowsAzurePowerShellTest
    {
        public StorageContainerTest()
            : base("Storage\\StorageContainer.ps1")
        {
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Storage)]
        [TestCategory("Scenario")]
        public void GetStorageContainerWithoutContainerName()
        {
            RunPowerShellTest("Test-GetAzureStorageContainerWithoutContainerName");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Storage)]
        [TestCategory("Scenario")]
        public void GetAzureStorageContainerWithPrefix()
        {
            RunPowerShellTest("Test-GetAzureStorageContainerWithPrefix");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Storage)]
        [TestCategory("Scenario")]
        public void NewAzureStorageContainer()
        {
            RunPowerShellTest("Test-NewAzureStorageContainer");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Storage)]
        [TestCategory("Scenario")]
        public void NewAzureStorageContainerWithPermission()
        {
            RunPowerShellTest("Test-NewAzureStorageContainerWithPermission");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Storage)]
        [TestCategory("Scenario")]
        public void NewExistsAzureStorageContainer()
        {
            RunPowerShellTest("Test-NewExistsAzureStorageContainer");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Storage)]
        [TestCategory("Scenario")]
        public void NewExistsAzureStorageContainerWithInvalidContainerName()
        {
            RunPowerShellTest("Test-NewExistsAzureStorageContainerWithInvalidContainerName");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Storage)]
        [TestCategory("Scenario")]
        public void RemoveAzureStorageContainer()
        {
            RunPowerShellTest("Test-RemoveAzureStorageContainer");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Storage)]
        [TestCategory("Scenario")]
        public void RemoveAzureStorageContainerByContainerPipeline()
        {
            RunPowerShellTest("Test-RemoveAzureStorageContainerByContainerPipeline");
        }
    }
}
