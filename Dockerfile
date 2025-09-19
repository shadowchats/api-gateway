# -----------------------------
# Stage 1: Build
# -----------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Shadowchats.ApiGateway.Presentation/Shadowchats.ApiGateway.Presentation.csproj ./Presentation/
RUN dotnet restore ./Presentation/Shadowchats.ApiGateway.Presentation.csproj

COPY Shadowchats.ApiGateway.Presentation ./Presentation

RUN dotnet publish ./Presentation/Shadowchats.ApiGateway.Presentation.csproj -c Release -o /app/publish

# -----------------------------
# Stage 2: Runtime
# -----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true

EXPOSE 5000

ENTRYPOINT ["dotnet", "Shadowchats.ApiGateway.Presentation.dll"]
