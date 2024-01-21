#!/bin/bash

SERVER_OUTPUT="server_out"
OUTPUT_EXEC_NAME="PPGModCompiler"
RESULT_EXEC_NAME="PPGMC_Server"

cd "$(dirname "$0")"
echo "Restoring packages..."
dotnet restore PPGModCompiler.csproj -a x64 > /dev/null
echo "Building..."
dotnet publish PPGModCompiler.csproj --configuration "Release" --output "$SERVER_OUTPUT" -a x64 > /dev/null
echo "Copying files..."
cp $SERVER_OUTPUT/$OUTPUT_EXEC_NAME $RESULT_EXEC_NAME
echo "Cleaning up..."
rm -rf $SERVER_OUTPUT
rm -rf bin
rm -rf obj
echo "Done!"
if [[ "$1" == "run" ]]; then
    echo "Running..."
    chmod +x ./$RESULT_EXEC_NAME
    ./$RESULT_EXEC_NAME
else
    echo ""
    echo "Did you know you can run './build.sh run' to run the executable upon build completion?"
fi