#!/usr/bin/env bash
npm --prefix=./Client install
npm --prefix=./Client run build
dotnet dev-certs https --clean
dotnet dev-certs https -t
dotnet restore Otm.sln
dotnet build Otm.sln