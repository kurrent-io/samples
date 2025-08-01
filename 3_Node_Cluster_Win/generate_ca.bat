@echo off
:: This script generates the root certificate and root private key for your EventStoreDB cluster on Windows.
:: Ensure you replace [Generate_certificate Path] with the actual path to your 'Generate_certificate' directory.

.\es-gencert-cli.exe create-ca -out [Generate_certificate Path]\ca
echo Root certificate and private key have been generated.

:: To generate certificates and private keys for each node, navigate to your 'Generate_certificate' directory.

:: Run the following commands, one for each node, adjusting paths, node numbers, and DNS names as necessary.

:: For example, for 'Node1' with certificate generator version 1.2.1:
:: .\es-gencert-cli create-node -ca-certificate \path\to\folder\Generate_certificate\es-gencert-cli_1.2.1_Windows-x86_64\ca\ca.crt -ca-key \path\to\folder\Generate_certificate\es-gencert-cli_1.2.1_Windows-x86_64\ca\ca.key -out \path\to\folder\Cluster\Node1\certificates -dns-names your.node1.dns.com

:: Repeat the above step for each node, ensuring to use the correct paths and DNS names.

:: After generating the certificates, configure each node by editing its configuration file. Include paths to its certificate, the private key, and the trusted root certificates. Refer to the provided configuration file example and adjust paths and settings as necessary.

pause
