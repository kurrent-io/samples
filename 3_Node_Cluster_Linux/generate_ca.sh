#!/bin/bash

# Generating Certificates Script

# Step 1: Create 'Generate_certificate' folder on the desktop

echo "Creating 'Generate_certificate' folder on the desktop..."
mkdir -p ~/Desktop/Generate_certificate
echo "Folder created."

# Step 2: Download and extract the certificate generator
# Note: This step requires manual download from the EventStore Certificate Generation CLI repository
# Visit: https://github.com/EventStore/es-gencert-cli/releases

echo "Please download the latest version of the certificate generator from the EventStore CLI repository and extract it into the 'Generate_certificate' folder."

# Step 3: Generating the root certificate and root private key
# Replace [Generate_certificate Path] with the actual path to your 'Generate_certificate' directory

echo "Generating the root certificate and root private key..."
cd ~/Desktop/Generate_certificate/es-gencert-cli
./es-gencert-cli create-ca -out ~/Desktop/Generate_certificate/ca
echo "Root certificate and private key generated."

# Instructions for generating node certificates
echo "To generate certificates and private keys for each node, run the following commands one at a time, adjusting the path and node number as necessary."

# Example command for Node1

echo "Example for Node1 (adjust paths and DNS names as needed):"
echo "./es-gencert-cli create-node -ca-certificate /path/to/Generate_certificate/es-gencert-cli_1.2.1_Linux-x86_64/ca/ca.crt -ca-key /path/to/Generate_certificate/es-gencert-cli_1.2.1_Linux-x86_64/ca/ca.key -out /path/to/Cluster/Node1/certificates -dns-names your.node1.dns.com"

# Reminder for including CA certificate and key paths in each node's configuration file
echo "Remember to include CA certificate and key paths in each node's configuration file as shown in the provided configuration file example."

