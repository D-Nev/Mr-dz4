FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY server/ChatServer.csproj server/
RUN dotnet restore server/ChatServer.csproj

COPY server/ server/
RUN dotnet publish server/ChatServer.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
COPY --from=build /out .
CMD ["dotnet", "ChatServer.dll"]
