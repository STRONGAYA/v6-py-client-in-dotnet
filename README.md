# Vantage6 Python Client in .NET

## Overview

This application is a .NET implementation of the Vantage6 Python client utilising PythonNet.   
It provides a bridge between .NET applications and the Vantage6 platform for federated data analysis.

## Features

- Connection to a Vantage6 central server
- Authentication with username and password
- End-to-End encryption support (given that the server requires it, and organisation Key code is provided)
- Multi-Factor Authentication (MFA) support (given that the server requires it, and an MFA code is provided)
- Creation and management of tasks
- Task result monitoring and retrieval

## Prerequisites

- .NET 8.0
- Python (version 3.10–3.12, _developed and tested in Python 3.10_)
- Python venv (this is normally included with Python installations)

_All other dependencies are installed through the application._

## Configuration

The application utilises `appsettings.json` for configuration. Complete the file with your settings, for example:
``json
{
  "Vantage6": {
    "Host": "https://strongaya-vantage6-server.eu",
    "Port": 443,
    "Username": "JanJanssen",
    "Password": "NotSoSecret",
    "MfaCode": "1A2B3C4D5E",
    "ApiPath": "/api",
    "PythonHome": "C:\\Users\\Jan\\AppData\\Local\\Programs\\Python\\Python310",
    "OrganizationKey": "C:\\Users\\Jan\\Documents\\Vantage6\\OrganisationKey.pem",
    "DefaultCollaborationId": 1,
    "DefaultOrganizationIds": [
      1
    ],
    "Vantage6Version": "4.10.0"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
``

## Usage

1. Ensure Python is properly installed and configured (we recommend adding the `python.exe` to your PATH)
2. Launch the application via an IDE such as VSCode or Jetbrains Rider or with a command line using `dotnet run`
3. The application will:
    - Connect to the Vantage6 server
    - Authenticate using the provided credentials
    - Execute the in `Program.cs` defined task
    - Retrieve and display the results

## Key Components

- `Vantage6Client`: Primary class for Vantage6 server interaction
- `PythonEnvironmentManager`: Manages Python runtime integration
- `Program.cs`: Contains the main logic and demonstrates client usage

## Error Handling

The application includes built-in error handling for:

- Connection issues
- Authentication errors
- Python runtime errors

## Contributors
- J. Hogenboom
- R. Carter
- V. Gouthamchand