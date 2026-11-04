# Сборка из родительской директории (рядом API и Common):
#   parent/
#     elist.api/          (этот репозиторий)
#     EList.Common/
#
#   cd parent
#   docker build -f elist.api/Dockerfile --build-arg PROJECT_DIR=elist.api -t crash240192/elist-api .

ARG PROJECT_DIR=elist.api

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG PROJECT_DIR
WORKDIR /src

COPY ${PROJECT_DIR}/ ./elist.api/
COPY EList.Common/ ./EList.Common/

WORKDIR /src/elist.api
RUN dotnet restore EList.sln
RUN dotnet publish EList.Api/EList.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app

RUN mkdir -p /app/logs \
    && chown -R app:app /app/logs

ENV ASPNETCORE_ENVIRONMENT=Production
# Override at runtime: -e ASPNETCORE_ENVIRONMENT=Staging
ENV ASPNETCORE_URLS=http://+:80

COPY --from=build /app/publish .

EXPOSE 80

USER app

ENTRYPOINT ["dotnet", "EList.Api.dll"]
