# -------- BUILD STAGE --------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything (simple + reliable for multi-project solutions)
COPY . .

# Restore + publish the Presentation project
RUN dotnet restore ./GPA/GPA.csproj
RUN dotnet publish ./GPA/GPA.csproj -c Release -o /app/publish /p:UseAppHost=false

# -------- RUNTIME STAGE --------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "GPA.dll"]