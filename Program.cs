// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Samples.Common;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;

namespace ConvertVirtualMachineToManagedDisks
{
    public class Program
    {
        private static ResourceIdentifier? _resourceGroupId = null;

        private static string userName = Utilities.CreateUsername();
        private static string password = Utilities.CreatePassword();
        private static AzureLocation region = AzureLocation.EastUS;

        /**
         * Azure Compute sample for managing virtual machines -
         *   - Create a virtual machine with un-managed OS and data disks
         *   - Deallocate the virtual machine
         *   - Migrate the virtual machine to use managed disk.
         */
        public static async Task RunSample(ArmClient client)
        {           
            var rgName = Utilities.CreateRandomName("rgCOMV");
            var storageName = Utilities.CreateRandomName("storage");
            var subnetName = Utilities.CreateRandomName("sub");
            var vnetName = Utilities.CreateRandomName("vnet");
            var nicName = Utilities.CreateRandomName("nic");
            var ipConfigName = Utilities.CreateRandomName("config");
            var linuxVmName = Utilities.CreateRandomName("VM1");

            try
            {
                //============================================================
                // Create resource group
                //
                var subscription = await client.GetDefaultSubscriptionAsync();
                var resourceGroupData = new ResourceGroupData(AzureLocation.SouthCentralUS);
                var resourceGroup = (await subscription.GetResourceGroups()
                    .CreateOrUpdateAsync(WaitUntil.Completed, rgName, resourceGroupData)).Value;
                _resourceGroupId = resourceGroup.Id;

                var storageData = new StorageAccountCreateOrUpdateContent(
                    new StorageSku(StorageSkuName.StandardLrs), StorageKind.StorageV2, region);
                var storage = (await resourceGroup.GetStorageAccounts()
                    .CreateOrUpdateAsync(WaitUntil.Completed, storageName, storageData)).Value;
                
                var vnetData = new VirtualNetworkData()
                {
                    Location = region,
                    AddressPrefixes = { "10.0.0.0/16" },
                    Subnets = { new SubnetData() { Name = subnetName, AddressPrefix = "10.0.0.0/28" } }
                };
                var vnet = (await resourceGroup.GetVirtualNetworks()
                    .CreateOrUpdateAsync(WaitUntil.Completed, vnetName, vnetData)).Value;
                var subnet = (await vnet.GetSubnets().GetAsync(subnetName)).Value;

                var nicData = new NetworkInterfaceData()
                {
                    Location = region,
                    IPConfigurations = {
                        new NetworkInterfaceIPConfigurationData()
                        {
                            Name = ipConfigName,
                            PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                            Primary = true,
                            Subnet = new SubnetData()
                            {
                                Id = subnet.Id
                            }
                        }
                    }
                };
                var nic = (await resourceGroup.GetNetworkInterfaces()
                    .CreateOrUpdateAsync(WaitUntil.Completed, nicName, nicData)).Value;

                //=============================================================
                // Create a Linux VM using a PIR image with un-managed OS and data disks

                Utilities.Log("Creating an un-managed Linux VM");

                var vmData = new VirtualMachineData(region)
                {
                    HardwareProfile = new VirtualMachineHardwareProfile()
                    {
                        VmSize = VirtualMachineSizeType.StandardDS1V2
                    },
                    OSProfile = new VirtualMachineOSProfile()
                    {
                        ComputerName = linuxVmName,
                        AdminUsername = userName,
                        AdminPassword = password
                    },
                    NetworkProfile = new VirtualMachineNetworkProfile()
                    {
                        NetworkInterfaces =
                        {
                            new VirtualMachineNetworkInterfaceReference()
                            {
                                Id = nic.Id,
                                Primary = true,
                            }
                        }
                    },
                    StorageProfile = new VirtualMachineStorageProfile()
                    {
                        OSDisk = new VirtualMachineOSDisk(DiskCreateOptionType.FromImage)
                        {
                            Name = linuxVmName,
                            OSType = SupportedOperatingSystemType.Linux,
                            Caching = CachingType.None,
                            VhdUri = new Uri($"https://{storageName}.blob.core.windows.net/vhds/{linuxVmName}.vhd")
                        },
                        DataDisks =
                        {
                            new VirtualMachineDataDisk(1, DiskCreateOptionType.Empty)
                            {
                                Name = "mydatadisk1",
                                DiskSizeGB = 100,
                                VhdUri =  new Uri($"https://{storageName}.blob.core.windows.net/vhds/mydatadisk1.vhd")
                            },
                            new VirtualMachineDataDisk(2, DiskCreateOptionType.Empty)
                            {
                                DiskSizeGB = 50,
                                Name = "mydatadisk2",
                                VhdUri =  new Uri($"https://{storageName}.blob.core.windows.net/vhds/mydatadisk2.vhd")
                            }
                        },
                        ImageReference = new ImageReference()
                        {
                            Publisher = "Canonical",
                            Offer = "UbuntuServer",
                            Sku = "16.04-LTS",
                            Version = "latest",
                        }
                    }
                };

                var linuxVM = (await resourceGroup.GetVirtualMachines()
                    .CreateOrUpdateAsync(WaitUntil.Completed, linuxVmName, vmData)).Value;

                Utilities.Log("Created a Linux VM with un-managed OS and data disks: " + linuxVM.Id);

                //=============================================================
                // Deallocate the virtual machine
                Utilities.Log("Deallocate VM: " + linuxVM.Id);

                await linuxVM.DeallocateAsync(WaitUntil.Completed);

                Utilities.Log("De-allocated VM: " + linuxVM.Id);

                //=============================================================
                // Migrate the virtual machine
                Utilities.Log("Migrate VM: " + linuxVM.Id);

                await linuxVM.ConvertToManagedDisksAsync(WaitUntil.Completed);

                Utilities.Log("Migrated VM: " + linuxVM.Id);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                try
                {
                    if (_resourceGroupId is not null)
                    {
                        Console.WriteLine($"Deleting Resource Group: {_resourceGroupId}");
                        await client.GetResourceGroupResource(_resourceGroupId).DeleteAsync(WaitUntil.Completed);
                        Console.WriteLine($"Deleted Resource Group: {_resourceGroupId}");
                    }
                }
                catch (Exception ex)
                {
                    Utilities.Log(ex);
                }
            }
        }

        public static async Task Main(string[] args)
        {
            try
            {
                //=================================================================
                // Authenticate
                var credential = new DefaultAzureCredential();

                var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
                // you can also use `new ArmClient(credential)` here, and the default subscription will be the first subscription in your list of subscription
                var client = new ArmClient(credential, subscriptionId);

                await RunSample(client);
            }
            catch (Exception ex)
            {
                Utilities.Log(ex);
            }
        }
    }
}
