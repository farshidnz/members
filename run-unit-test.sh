#!/bin/bash

dotnet test --logger:"junit;LogFilePath=/app/testout/{assembly}.xml" SettingsAPI.Tests/SettingsAPI.Tests.csproj

chown -R $1 /app