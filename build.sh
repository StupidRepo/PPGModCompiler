#!/bin/bash

SERVER_OUTPUT="server_out"
OUTPUT_EXEC_NAME="PPGModCompiler"

cd "$(dirname "$0")"
echo "Restoring packages..."
dotnet restore PPGModCompiler.csproj -a x64 > /dev/null
echo "Building..."
dotnet publish PPGModCompiler.csproj --configuration "Release" --output "$SERVER_OUTPUT" -a x64 > /dev/null
echo "Copying files..."
cp $SERVER_OUTPUT/$OUTPUT_EXEC_NAME PPGMC_Server
echo "Cleaning up..."
rm -rf $SERVER_OUTPUT
rm -rf bin
rm -rf out
rm -rf obj
echo "Done!"