FROM microsoft/aspnetcore-build:2

WORKDIR /build
COPY ./IRIPrometheusExporter /build
RUN cd /build && dotnet publish -c Release -o /app

#####################################

FROM microsoft/aspnetcore:2

ENV IRI_API_URI=http://localhost:14265
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:5000

WORKDIR /app
COPY --from=0 /app /app

EXPOSE 5000

CMD ["dotnet", "/app/IRIPrometheusExporter.dll"]