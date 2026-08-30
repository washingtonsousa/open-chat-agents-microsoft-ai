FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/OpenChatAgents.Api/OpenChatAgents.Api.csproj src/OpenChatAgents.Api/
COPY src/OpenChatAgents.Infrastructure/OpenChatAgents.Infrastructure.csproj src/OpenChatAgents.Infrastructure/
RUN dotnet restore src/OpenChatAgents.Api/OpenChatAgents.Api.csproj
COPY src/OpenChatAgents.Api/ src/OpenChatAgents.Api/
COPY src/OpenChatAgents.Infrastructure/ src/OpenChatAgents.Infrastructure/
RUN dotnet publish src/OpenChatAgents.Api/OpenChatAgents.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8090
EXPOSE 8090
ENTRYPOINT ["dotnet", "OpenChatAgents.Api.dll"]
