# Function Key Configurator

A .NET console application that automates the management of function keys for Azure Functions. This tool simplifies the process of creating and updating function keys, ensuring secure access to your Azure Functions.

This application leverages an Azure Resource Manager (ARM) client to interact with Azure resources, providing a robust solution for managing function keys programmatically. It is designed to be flexible and customisable, allowing users to define their own roles and keys through configuration files.

Key Features:
- Automated function key management
- Secure access control for Azure Functions
- Customisable role-based key configuration
- Easy integration with Azure Resource Manager
- Support for multiple Azure regions and subscriptions
- Flexible configuration options
- Command-line interface for easy use
- Extensible role-based access control

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/sweetfa/FunctionKeyConfigurator.git
   cd FunctionKeyConfigurator
   ```  
2. Install dependencies:
   ```bash
   dotnet restore
   ```
3. Configure Azure credentials:

      • Configuration: Set the following environment variables in your shell or Rider run configuration:
   
         ◦ AZURE_CLIENT_ID: Your Service Principal App ID.
   
         ◦ AZURE_TENANT_ID: Your Azure AD Tenant ID.
   
         ◦ AZURE_CLIENT_SECRET: Your Service Principal Client Secret.
   
         • Permission: The Service Principal must have at least Contributor or Website
           Contributor permissions on the Function App or Resource Group, as well as 
           Key Vault Secrets Officer on the Key Vault.
   
4. Build and run the application:
   ```bash
   dotnet run
   ```

# Azure Authentication

To get an Azure Client ID (also known as an Application ID), you need to register an application within your Azure Active Directory (now called Microsoft Entra ID). This Client ID acts as the "username" for your application or script when it tries to authenticate with Azure.

Here is the step-by-step process to get one:

1. Register the Application
   Log in to the Azure Portal.

   Search for and select Microsoft Entra ID (formerly Azure Active Directory).
   
   In the left-hand sidebar, click on App registrations.
   
   Click + New registration.
   
   Name: Give it a meaningful name (e.g. FunctionAppFunctionKeyConfigurator).
   
   Supported account types: Usually, "Accounts in this organisational directory only" is enough for internal tools.
   
   Redirect URI: You can leave this blank for now if you are just writing a back-end script or CLI tool.
   
   Click Register.


2. Locate the Client ID
   Once the registration is complete, you will be taken to the Overview page for that application.

   Look for the field labelled Application (client) ID.
   
   It will be a GUID (e.g. 12345678-abcd-1234-efgh-1234567890ab).
   
   Copy this value. This is your Client ID.


3. Additional Requirements for Authentication
   A Client ID alone is usually not enough to log in programmatically. You will almost always need two other pieces of information:

   Directory (tenant) ID: Found on the same Overview page as the Client ID.
   
   Client Secret:
   
   Click Certificates & secrets in the left sidebar.
   
   Click + New client secret.
   
   Copy the Value immediately (it will be hidden forever once you leave the page).

# Running the application

1. Navigate to the FunctionKeyConfigurator folder.
2. Add the roles and keys to the configuration file config.json.
3. Run the application using the command 

   ```
   dotnet bin/Debug/net9.0/FunctionKeyConfigurator.dll
   ```

