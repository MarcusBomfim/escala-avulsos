# As imagens da escala, de um Dockerfile só (ADR-0023): a API, o migrador do
# banco e o aplicativo. O .NET compila uma vez; cada alvo copia o que é seu,
# sobre uma base sem shell e sem root.
#
#   docker build --target api .
#
# As bases vão fixadas pelo digest: a mesma tag pode apontar para outra imagem
# amanhã, o digest não. Atualizar uma base é trocar o digest dela. São as
# mesmas do crm-obras e do planejamento-obras.
#
# A base do ASP.NET é a -extra, com a base de fusos, que a chiseled comum não
# traz: a escala inteira é no relógio do porto (America/Sao_Paulo), e sem ele
# a API não sobe.

ARG SDK=mcr.microsoft.com/dotnet/sdk:10.0.402-noble@sha256:0d99ebf1ccad15037b5c98c9fe8979d6b8dbac23812feae2277a0b277b5c5ffa
ARG ASPNET=mcr.microsoft.com/dotnet/aspnet:10.0.13-noble-chiseled-extra@sha256:9e3919aff68057f3e45125c5255a71d3b5f40364e45ede883348688851b9f0df
ARG RUNTIME_DEPS=mcr.microsoft.com/dotnet/runtime-deps:10.0.13-noble-chiseled@sha256:8d23bb82cd8345ee51738b36c6da8e2f89ded7a547bc25fa52d5cc60e37f6d7a
ARG NODE=node:24.22.0-alpine3.24@sha256:44fce696f38ba8c987c4db89954051000bbd05ffd8d8659ba15ed4a8602e937f
ARG NGINX=nginxinc/nginx-unprivileged:1.30.6-alpine3.24@sha256:782bb89768cc87b7f95c18b95baa1af250e5d451f26794f59363540161147c01

# --- .NET: as dependências primeiro, para a camada delas só refazer quando mudam
FROM ${SDK} AS restauracao
WORKDIR /fonte
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig dotnet-tools.json ./
COPY src/EscalaAvulsos.Dominio/EscalaAvulsos.Dominio.csproj src/EscalaAvulsos.Dominio/
COPY src/EscalaAvulsos.Infraestrutura/EscalaAvulsos.Infraestrutura.csproj src/EscalaAvulsos.Infraestrutura/
COPY src/EscalaAvulsos.Observabilidade/EscalaAvulsos.Observabilidade.csproj src/EscalaAvulsos.Observabilidade/
COPY src/EscalaAvulsos.Api/EscalaAvulsos.Api.csproj src/EscalaAvulsos.Api/
RUN dotnet restore src/EscalaAvulsos.Api \
    && dotnet tool restore

# --- .NET: a compilação, com a versão e o commit que o OpenTelemetry vai mostrar
FROM restauracao AS publicacao
ARG VERSAO=0.1.0-local
ARG REVISAO=
COPY src/ src/
RUN dotnet publish src/EscalaAvulsos.Api -c Release --no-restore -o /saida/api \
        -p:Version="$VERSAO" -p:SourceRevisionId="$REVISAO" -p:UseAppHost=false

# --- As migrações num executável só, que não precisa de SDK para rodar
FROM restauracao AS pacote-de-migracoes
COPY src/ src/
RUN dotnet ef migrations bundle --project src/EscalaAvulsos.Infraestrutura \
        --configuration Release --self-contained --target-runtime linux-x64 --output /saida/migrador/efbundle

# --- O aplicativo de produção
FROM ${NODE} AS aplicativo
WORKDIR /web
COPY web/package.json web/package-lock.json ./
RUN npm ci --no-fund --no-audit
COPY web/ ./
RUN npm run build

# --- As imagens

FROM ${ASPNET} AS api
LABEL org.opencontainers.image.title="escala-avulsos-api" \
      org.opencontainers.image.description="A API REST da escala dos avulsos, com os avisos e o tempo real."
WORKDIR /app
COPY --from=publicacao /saida/api .
ENTRYPOINT ["dotnet", "EscalaAvulsos.Api.dll"]

# Roda as migrações e sai. A cadeia de conexão vem de ConnectionStrings__Escala.
FROM ${RUNTIME_DEPS} AS migrador
LABEL org.opencontainers.image.title="escala-avulsos-migrador" \
      org.opencontainers.image.description="As migrações do banco, num executável que roda e sai."
WORKDIR /app
COPY --from=pacote-de-migracoes /saida/migrador/efbundle .
ENTRYPOINT ["/app/efbundle"]

FROM ${NGINX} AS web
LABEL org.opencontainers.image.title="escala-avulsos-web" \
      org.opencontainers.image.description="O aplicativo do trabalhador, do escalador e do operador, servido pelo nginx, com /api repassado para a API."
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=aplicativo /web/dist /usr/share/nginx/html
