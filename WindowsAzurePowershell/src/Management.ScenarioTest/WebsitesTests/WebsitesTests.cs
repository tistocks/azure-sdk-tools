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

namespace Microsoft.WindowsAzure.Management.ScenarioTest.WebsitesTests
{
    using System.IO;
    using Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class WebsitesTests : WindowsAzurePowerShellTest
    {
        private string currentDirectory;

        public WebsitesTests()
            : base("Websites\\Common.ps1",
                   "Websites\\WebsitesTests.ps1")
        {

        }

        [TestInitialize]
        public override void TestSetup()
        {
            base.TestSetup();
            powershell.AddScript("Initialize-WebsiteTest");
            currentDirectory = Directory.GetCurrentDirectory();
        }

        [TestCleanup]
        public override void TestCleanup()
        {
            base.TestCleanup();
            Directory.SetCurrentDirectory(currentDirectory);
        }

        #region Remove-AzureWebsite Scenario Tests

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        public void TestRemoveAzureWebsiteWithInvalidCredentials()
        {
            RunPowerShellTest("Test-WithInvalidCredentials {Remove-AzureWebsite $(Get-WebsiteName) }");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        public void TestRemoveAzureServiceWithValidName()
        {
            RunPowerShellTest("Test-RemoveAzureServiceWithValidName");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        public void TestRemoveAzureServiceWithNonExistingName()
        {
            RunPowerShellTest("Test-RemoveAzureServiceWithNonExistingName");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        public void TestRemoveAzureServiceWithWhatIf()
        {
            RunPowerShellTest("Test-RemoveAzureServiceWithWhatIf");
        }

        #endregion

        #region Get-AzureWebsiteLog Scenario Tests

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        public void TestGetAzureWebsiteLogWithInvalidCredentials()
        {
            RunPowerShellTest("Test-WithInvalidCredentials { Get-AzureWebsiteLog -Tail -Name $(Get-WebsiteName) }");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        public void TestGetAzureWebsiteLogTail()
        {
            RunPowerShellTest("Test-GetAzureWebsiteLogTail");
        }

        [TestMethod]
        [TestCategory(Category.All)]
        [TestCategory(Category.Websites)]
        [Ignore] // https://github.com/WindowsAzure/azure-sdk-tools/issues/1177
        public void TestGetAzureWebsiteLogTailPath()
        {
            RunPowerShellTest("Test-GetAzureWebsiteLogTailPath");
        }
        

        #endregion
    }
}
