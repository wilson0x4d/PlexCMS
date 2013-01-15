$nuget = [System.IO.Path]::Combine([System.Environment]::GetFolderPath("LocalApplicationData"), "NuGet\NuGet.exe")
set-alias nuget $nuget
nuget pack "PlexCMS-Web-PreInstall.nuspec"
nuget pack "PlexCMS-Web.nuspec"