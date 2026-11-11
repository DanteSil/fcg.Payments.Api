FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY PaymentsAPI.csproj ./
RUN dotnet restore PaymentsAPI.csproj
COPY . ./
RUN dotnet publish PaymentsAPI.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "PaymentsAPI.dll"]
