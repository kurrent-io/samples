# Certificate Generation Script for EventStoreDB

This script automates the process of generating root and node certificates for EventStoreDB clusters on Linux environments. Follow the instructions below to download, prepare, and execute the script.

## Instructions for use

### Download the script

1. Save the script to a file, for example, `generate_certs.sh`.

### Make it executable

2. Change the file's permissions to make it executable by running:

```bash
chmod +x generate_certs.sh

```
### Execute the script by running:

`./generate_certs.sh`.

## Notes

- The script will prompt you to manually download the certificate generator from the Event Store CLI repository. 
- You will need to replace placeholder paths within the script with actual paths relevant to your envrionment. 
- Given the manual download step and the necessity for user input for specific paths and DNS names, this script serves primarily as an instructional guide. 

### Additional resources 

For further details on setting up EventStoreDB clusters and configuring them with generated certificates, refer to the official EventStore documentation:

- [EventStore Certificate Generation CLI repository](https://github.com/EventStore/es-gencert-cli/releases)
- [cluster with DNS guide](https://developers.eventstore.com/server/v23.10/cluster.html#cluster-with-dns)