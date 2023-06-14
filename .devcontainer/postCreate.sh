#!/usr/bin/env bash
npm --prefix=./Client install
npm --prefix=./Client run build
dotnet restore Otm.sln
dotnet build Otm.sln