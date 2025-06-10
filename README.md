# Vantage6 Python Client in .NET

## Overview

This application is a .NET implementation of the Vantage6 Python client utilising PythonNet.   
It provides a bridge between .NET applications and the Vantage6 platform for federated data analysis.

## Features

- Connection to a Vantage6 central server
- Authentication with username and password
- End-to-End encryption support (given that the Vantage6 server requires it, and organisation Key code is provided)
- Multi-Factor Authentication (MFA) support (given that the Vantage6 server requires it, and an MFA code is provided)
- Creation and management of tasks
- Task result monitoring and retrieval

## Prerequisites

- .NET 8.0
- Python (version 3.10–3.12, _developed and tested in Python 3.10_)
- Python venv (this is normally included with Python installations)

_All other dependencies are installed through the application._

## Configuration

The application utilises `appsettings.json` for configuration. Complete the file with your settings, for example:

```json
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
```

## Defining your tasks' algorithm input

The application utilises a simple JSON structure to define the algorithm input for the federated tasks.  
The name of the JSON file can be arbitrary, and it should be provided to the application as a parameter when running the
application.

The JSON structure should provide you with the necessary flexibility to send any task to the Vantage6 infrastructure.  
Ensure that the following JSON file adheres to the following structure:

```json
{
  "name": "Name to identify your task",
  "description": "Some details regarding what you are doing for traceability",
  "databaseLabels": "database label, in most scenario 'default' is sufficient",
  "image": "Docker image of the desired Vantage6 algorithm",
  "input": {
    "method": "The central function of the algorithm",
    "kwargs": {
      "algorithm_relevant_input_parameter_1": "relevant_value_1"
    }
  },
  "algorithm_repository": "https://github.com/your-organisation/your-algorithm-repository", 
  "algorithm_wiki": "https://github.com/your-organisation/your-algorithm-repository/something-to-guide-users"
}
```

Additionally, there is an example that retrieves the average of 'Age' in `/examples/algorithm_input/general/average_age.json`.

## Usage

1. Ensure Python is properly installed and configured (we recommend adding the `python.exe` to your PATH)
2. Define the algorithm input of the task you would like to run as outlined above, and save it as a JSON file (e.g., `algorithm_input.json`).
   You can find an example in `/examples/algorithm_input/average_age.json`.
2. Launch the application via an IDE such as VSCode or Jetbrains Rider (given you have set up the
   `path/to/algorithm_input.json` as parameter for your run) or with a command line using
   `dotnet run -- path/to/algorithm_input.json`
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