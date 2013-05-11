param (
	[switch] $publish
)

$packageName = "PlexCMS-Web";

# prep folder for package creation
$outputPath = $((pwd).Path + "\..\Packages");
$void = New-Item -ItemType Container -Path $outputPath -Force
		
# lift version info
$assemblyPath = $((pwd).Path + "\$packageName\bin\Debug\$packageName.dll");
echo $assemblyPath
$assemblyName = [Reflection.AssemblyName]::GetAssemblyName($assemblyPath);
$assemblyVersion =  $AssemblyName.Version;

# package
$nuget = $((pwd).Path + "\.nuget\nuget.exe");
set-alias nuget $nuget
nuget pack "$packageName-PreInstall.nuspec" -Output "$outputPath" -Version $assemblyVersion
nuget pack "$packageName.nuspec" -Output "$outputPath" -Version $assemblyVersion

# publish
if ($publish)
{
	nuget push "$outputPath\$packageName-PreInstall.$assemblyVersion.nupkg"
	nuget push "$outputPath\$packageName.$assemblyVersion.nupkg"
}
