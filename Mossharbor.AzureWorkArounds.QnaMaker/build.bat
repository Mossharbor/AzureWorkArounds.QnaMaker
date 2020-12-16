REM msbuild /t:pack /p:NuspecFile=Package.nuspec Mossharbor.AzureWorkArounds.QnaMaker.csproj
dotnet build
dotnet pack -p:NuspecFile=.\Package.nuspec