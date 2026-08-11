FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["nuget.config", "./"]
COPY ["nupkg/", "nupkg/"]
COPY ["Grocery.ServiceHub/Grocery.ServiceHub.csproj", "Grocery.ServiceHub/"]
COPY ["Grocery.Context/Grocery.Context.csproj", "Grocery.Context/"]
COPY ["Grocery.DMO/Grocery.DMO.csproj", "Grocery.DMO/"]
COPY ["Grocery.DTO/Grocery.DTO.csproj", "Grocery.DTO/"]
RUN dotnet restore "Grocery.ServiceHub/Grocery.ServiceHub.csproj"

# Copy source code
COPY . .

WORKDIR "/src/Grocery.ServiceHub"
RUN dotnet build "Grocery.ServiceHub.csproj" -c Debug -o /app/build

FROM build AS publish
RUN dotnet publish "Grocery.ServiceHub.csproj" -c Debug -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Grocery.ServiceHub.dll"]
