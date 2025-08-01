# EventStoreDB Certificate Generation Guide for Windows

This guide assists Windows users in generating root and node certificates for EventStoreDB clusters using a batch script. Follow the steps below to use the script effectively.

## Prerequisites

Before you begin, ensure you have downloaded the `es-gencert-cli` tool from the EventStore Certificate Generation CLI repository. This tool is required to generate the certificates.

- [EventStore Certificate Generation CLI Repository](https://github.com/EventStore/es-gencert-cli/releases)

## Instructions for use

### 1. Prepare the script

Save the provided script into a file named `generate_certs.bat`.

### 2. Download and extract the certificate generator

Manually download the latest version of the certificate generator (`es-gencert-cli`) from the EventStore Certificate Generation CLI repository. Extract the downloaded archive into a folder named `Generate_certificate` on your desktop or another preferred location.

### 3. Edit the script to reflect your paths

Before running the script, you need to edit `[Generate_certificate Path]` within the script to match the actual path where you extracted the `es-gencert-cli` tool. This adjustment is crucial for the script to function correctly.

### 4. Run the script

Right-click on `generate_certs.bat` and select "Run as administrator." This step is necessary to ensure the script has sufficient permissions to generate certificates.

### Generating node certificates

After generating the root certificate, the script will instruct you to generate certificates and private keys for each node. You must adjust the paths, node numbers, and DNS names as necessary for your specific configuration.

For example:

```bat
.\es-gencert-cli.exe create-node -ca-certificate \path\to\folder\Generate_certificate\es-gencert-cli_1.2.1_Windows-x86_64\ca\ca.crt -ca-key \path\to\folder\Generate_certificate\es-gencert-cli_1.2.1_Windows-x86_64\ca\ca.key -out \path\to\folder\Cluster\Node1\certificates -dns-names your.node1.dns.com
```
Repeat this process for each node in your cluster, ensuring the paths and DNS names are correctly configured. 


### Configuring each node

After generating the certificates, you'll need to configure each node by editing its configuration file to include the paths to tis certificate, private key, and the trusted root certificates. Adjust the paths and settings as necessary, based on the example configuration provided in the script comments. 


## Notes

- The script will prompt you to manually download the certificate generator from the Event Store CLI repository. 
- You will need to replace placeholder paths within the script with actual paths relevant to your envrionment. 
- Given the manual download step and the necessity for user input for specific paths and DNS names, this script serves primarily as an instructional guide. 

### Additional resources 

For further details on setting up EventStoreDB clusters and configuring them with generated certificates, refer to the official EventStore documentation:

- [EventStore Certificate Generation CLI repository](https://github.com/EventStore/es-gencert-cli/releases)
- [cluster with DNS guide](https://developers.eventstore.com/server/v23.10/cluster.html#cluster-with-dns)