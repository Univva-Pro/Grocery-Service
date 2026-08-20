#!/usr/bin/env bash
set -e

echo -e "\033[0;36m====================================================\033[0m"
echo -e "\033[0;36m Starting Grocery-Service on Linux (Port 8089)...\033[0m"
echo -e "\033[0;36m====================================================\033[0m"

# Restore & Build
dotnet restore ./Grocery.ServiceHub/Grocery.ServiceHub.csproj
dotnet build ./Grocery.ServiceHub/Grocery.ServiceHub.csproj -c Release

# Run Kestrel Server on Linux
export ASPNETCORE_URLS="http://0.0.0.0:8089"
export MongoDb__ConnectionString="${MongoDb__ConnectionString:-mongodb+srv://naikamit6773_db_user:kF6dl6BlSEf53mZ6@cluster0.r92gitf.mongodb.net/?appName=Cluster0}"
export MongoDb__DatabaseName="GroceryDB"

dotnet run --project ./Grocery.ServiceHub/Grocery.ServiceHub.csproj -c Release
