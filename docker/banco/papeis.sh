#!/bin/sh
# O papel da API, como quem implanta o criaria (ADR-0012, ADR-0023). Roda uma
# vez, quando o volume do banco nasce, antes das migrações: as tabelas ainda
# não existem. Os privilégios padrão concedem o que o dono — quem migra —
# criar depois no esquema: a API lê e grava as tabelas, e só. Mudar o esquema
# é com o migrador.
#
# Sem permissão de execução — no Linux, o arquivo chega assim do Git —, o
# entrypoint do PostgreSQL não roda o script: lê no shell dele. Por isso só o
# set -e, que ele mesmo usa, e nada que mude o comportamento do resto dele.
set -e

psql --set ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
    --set senha_da_aplicacao="$SENHA_DA_APLICACAO" <<'SQL'
create role escala_aplicacao login password :'senha_da_aplicacao';

create schema escala;
grant usage on schema escala to escala_aplicacao;

alter default privileges in schema escala
    grant select, insert, update, delete on tables to escala_aplicacao;
SQL
