#!/bin/bash
set -e

# Wait for the PostgreSQL server to be available
./wait-for-it.sh db:5432 -t 30 -- echo "Database is up"

echo "Starting the application..."
exec dotnet Web.Api.dll
