#!/bin/sh

# Get absolute path this script is in and use this path as a base for all other (relatve) filenames.
# !! Please make sure there are no spaces inside the path !!
# Source: https://stackoverflow.com/questions/242538/unix-shell-script-find-out-which-directory-the-script-file-resides
SCRIPT=$(readlink -f "$0")
SCRIPTPATH=$(dirname "$SCRIPT")
ROOT_PATH=$(cd ${SCRIPTPATH}/../; pwd)

cd ${ROOT_PATH}

# exclude the Testhelper project:  [TestHelper]*
# exclude all tests projects:      [*.Test]Otm.*
COVERLET_EXCLUDE_FILTER=[TestHelper]*,[*.Test]Otm.*,[*.Tests]Otm.*,[xunit.*]*
COVERLET_INCLUDE_FILTER=[*]Otm.*,[Otm*]*

echo Testing project: Otm.Test.csproj
	
echo dotnet test //p:CollectCoverage=true //p:CoverletOutputFormat=lcov //p:CoverletOutput=lcov.info /p:Exclude=\"$COVERLET_EXCLUDE_FILTER\" /p:Include=\"$COVERLET_INCLUDE_FILTER\" //p:configuration=Release ./tests/Otm.Test/Otm.Test.csproj
dotnet test //p:CollectCoverage=true //p:CoverletOutputFormat=lcov //p:CoverletOutput=lcov.info /p:Exclude=\"$COVERLET_EXCLUDE_FILTER\" /p:Include=\"$COVERLET_INCLUDE_FILTER\" //p:configuration=Release ./tests/Otm.Test/Otm.Test.csproj

cp ${ROOT_PATH}/tests/Otm.Test/lcov.info ${ROOT_PATH}/src/Otm/