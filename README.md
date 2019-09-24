---
page_type: sample
languages:
- csharp
products:
- azure
extensions:
- services: Compute
- platforms: dotnet
description: "Azure Compute sample for managing virtual machines."
---

# Getting started with converting Virtual Machines to use Managed Disks #

 Azure Compute sample for managing virtual machines -
   - Create a virtual machine with un-managed OS and data disks
   - Deallocate the virtual machine
   - Migrate the virtual machine to use managed disk.


## Running this Sample ##

To run this sample:

Set the environment variable `AZURE_AUTH_LOCATION` with the full path for an auth file. See [how to create an auth file](https://github.com/Azure/azure-libraries-for-net/blob/master/AUTH.md).

    git clone https://github.com/Azure-Samples/managed-disk-dotnet-convert-existing-virtual-machines-to-use-managed-disks.git

    cd managed-disk-dotnet-convert-existing-virtual-machines-to-use-managed-disks

    dotnet build

    bin\Debug\net452\ConvertVirtualMachineToManagedDisks.exe

## More information ##

[Azure Management Libraries for C#](https://github.com/Azure/azure-sdk-for-net/tree/Fluent)
[Azure .Net Developer Center](https://azure.microsoft.com/develop/net/)
If you don't have a Microsoft Azure subscription you can get a FREE trial account [here](http://go.microsoft.com/fwlink/?LinkId=330212)

---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
